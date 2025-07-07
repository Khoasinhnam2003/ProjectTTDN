import { useState } from 'react';
import { queryApi, commandApi } from '../api';
import { useNavigate } from 'react-router-dom';

function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      let response = await queryApi.post('api/Authentication/login', {
        username,
        password,
      });

      if (!response.data || !response.data.isSuccess) {
        response = await commandApi.post('api/Authentication/login', {
          username,
          password,
        });
      }

      console.log('Full Response from API:', response);
      console.log('Response data:', JSON.stringify(response.data, null, 2));

      if (!response.data || typeof response.data !== 'object') {
        throw new Error('Phản hồi từ server không hợp lệ.');
      }

      if (!response.data.isSuccess) {
        throw new Error(response.data.error?.message || 'Đăng nhập thất bại');
      }

      const data = response.data.data;
      if (!data || typeof data !== 'object') {
        throw new Error('Dữ liệu đăng nhập không hợp lệ.');
      }

      const { userId, employeeId, username: userName, token, roles } = data;
      const roleArray = roles && roles.$values ? roles.$values : roles || [];

      const userData = { userId, employeeId, username: userName, roles: roleArray };
      console.log('User data to save:', userData);

      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(userData));

      if (roleArray.includes('Admin')) {
        navigate('/admin-dashboard');
      } else if (roleArray.includes('User')) {
        navigate('/user-dashboard');
      } else {
        throw new Error('Vai trò không được hỗ trợ.');
      }
    } catch (err) {
      setError(err.message || 'Đăng nhập thất bại. Vui lòng kiểm tra thông tin.');
      console.error('Lỗi chi tiết:', err.response ? err.response.data : err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="bg-white p-4 rounded shadow w-100" style={{ maxWidth: '400px' }}>
        <h2 className="text-center mb-4">Đăng nhập</h2>
        {error && <div className="alert alert-danger">{error}</div>}
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