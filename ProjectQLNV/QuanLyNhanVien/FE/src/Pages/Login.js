import { useState } from 'react';
import { commandApi } from '../api';
import { useNavigate } from 'react-router-dom';

function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [checkInTime, setCheckInTime] = useState(null);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      // Gửi cả camelCase và PascalCase để tương thích với BE
      const requestBody = {
        username,
        password,
        Username: username,
        Password: password,
      };

      console.log('Attempting login with commandApi...', requestBody);
      const response = await commandApi.post('api/Authentication/login', requestBody);

      console.log('Full Response from API:', JSON.stringify(response, null, 2));

      if (!response.data || typeof response.data !== 'object') {
        throw new Error('Phản hồi từ server không hợp lệ.');
      }

      if (!response.data.isSuccess) {
        throw new Error(response.data.error?.message || 'Đăng nhập thất bại');
      }

      // Kiểm tra cả response.data và response.data.data
      const data = response.data.data || response.data;

      // Xử lý cả camelCase và PascalCase
      const userId = data.userId || data.UserId;
      const employeeId = data.employeeId ?? data.EmployeeId ?? 0; // Cho phép employeeId là 0
      const userName = data.username || data.Username;
      const token = data.token || data.Token;
      const roles = data.roles || data.Roles;
      const checkInTime = data.checkInTime || data.CheckInTime;

      if (!userId || !token) {
        console.error('Missing fields:', { userId, employeeId, userName, token, roles, checkInTime });
        throw new Error('Thiếu dữ liệu người dùng bắt buộc (userId hoặc token).');
      }

      const roleArray = Array.isArray(roles) ? roles : (roles?.$values || []);
      if (!roleArray.length) {
        throw new Error('Không có vai trò nào được gán.');
      }

      const userData = { userId, employeeId, username: userName, roles: roleArray };
      console.log('Dữ liệu người dùng để lưu:', userData);

      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(userData));

      // Lưu và hiển thị CheckInTime
      if (checkInTime) {
        setCheckInTime(checkInTime);
        localStorage.setItem('checkInTime', checkInTime);
      } else {
        console.warn('No CheckInTime received from API.');
      }

      if (roleArray.includes('Admin')) {
        navigate('/admin-dashboard');
      } else if (roleArray.includes('User')) {
        navigate('/user-dashboard');
      } else if (roleArray.includes('Manager')) {
        navigate('/manager-dashboard');
      } else {
        throw new Error('Vai trò không được hỗ trợ.');
      }
    } catch (err) {
      console.error('commandApi error response:', JSON.stringify(err.response?.data, null, 2));
      setError(err.response?.data?.error?.message || err.message || 'Đăng nhập thất bại. Vui lòng kiểm tra thông tin đăng nhập.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="bg-white p-4 rounded shadow w-100" style={{ maxWidth: '400px' }}>
        <h2 className="text-center mb-4">Đăng nhập</h2>
        {error && <div className="alert alert-danger">{error}</div>}
        {checkInTime ? (
          <div className="alert alert-info mt-2">
            Thời gian check-in: {new Date(checkInTime).toLocaleString('vi-VN', {
              timeZone: 'Asia/Ho_Chi_Minh',
              dateStyle: 'short',
              timeStyle: 'medium',
            })}
          </div>
        ) : null}
        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label htmlFor="username" className="form-label">Tên đăng nhập</label>
            <input
              type="text"
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="form-control"
              placeholder="Nhập tên đăng nhập"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="password" className="form-label">Mật khẩu</label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="form-control"
              placeholder="Nhập mật khẩu"
              required
              disabled={loading}
            />
          </div>
          <button type="submit" className="btn btn-primary w-100" disabled={loading}>
            {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
        </form>
        <p className="text-center text-muted mt-3">
          Chưa có tài khoản? <a href="/register" className="text-primary">Đăng ký</a>
        </p>
      </div>
    </div>
  );
}

export default Login;