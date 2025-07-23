import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { commandApi } from '../api';

function Register() {
  const [formData, setFormData] = useState({
    employeeId: '',
    username: '',
    password: '',
    roleIds: []
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  // Danh sách vai trò cứng nhắc cho mục đích đơn giản
  // Trong thực tế, bạn có thể gọi API để lấy danh sách vai trò từ backend
  const availableRoles = [
    { roleId: 2, name: 'User' },
    { roleId: 3, name: 'Manager' }
  ];

  const validateForm = () => {
    if (!formData.employeeId || isNaN(parseInt(formData.employeeId)) || parseInt(formData.employeeId) <= 0) {
      return 'Mã nhân viên phải là một số lớn hơn 0.';
    }
    if (!formData.username) {
      return 'Tên đăng nhập không được để trống.';
    }
    if (formData.username.length > 50) {
      return 'Tên đăng nhập tối đa 50 ký tự.';
    }
    if (!/^[a-zA-Z0-9_]+$/.test(formData.username)) {
      return 'Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới.';
    }
    if (!formData.password) {
      return 'Mật khẩu không được để trống.';
    }
    if (formData.password.length < 8) {
      return 'Mật khẩu phải có ít nhất 8 ký tự.';
    }
    if (!/^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$/.test(formData.password)) {
      return 'Mật khẩu phải chứa ít nhất một chữ cái và một số.';
    }
    if (!Array.isArray(formData.roleIds)) {
      return 'Danh sách vai trò không hợp lệ.';
    }
    if (formData.roleIds.length > 0 && !formData.roleIds.every(id => availableRoles.some(role => role.roleId === id))) {
      return 'Một hoặc nhiều vai trò được chọn không hợp lệ.';
    }
    return null;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setError('');
  };

  const handleRoleChange = (e) => {
    const roleId = parseInt(e.target.value, 10);
    const isChecked = e.target.checked;
    setFormData((prev) => ({
      ...prev,
      roleIds: isChecked
        ? [...prev.roleIds, roleId]
        : prev.roleIds.filter((id) => id !== roleId)
    }));
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      setLoading(false);
      return;
    }

    try {
      const payload = {
        employeeId: parseInt(formData.employeeId, 10),
        username: formData.username,
        password: formData.password,
        roleIds: formData.roleIds
      };
      console.log('Sending payload:', JSON.stringify(payload, null, 2));

      const response = await commandApi.post('api/Users', payload);

      console.log('API Response:', JSON.stringify(response.data, null, 2));

      if (response.data && response.data.isSuccess) {
        alert('Tài khoản đã được tạo thành công!');
        navigate('/');
      } else {
        throw new Error(response.data.error?.message || 'Đăng ký thất bại.');
      }
    } catch (err) {
      console.error('Lỗi chi tiết:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
      if (err.response?.status === 400 && err.response.data.errors) {
        const errorMessages = Object.entries(err.response.data.errors)
          .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
          .join('; ');
        setError(errorMessages || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.');
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Không có quyền truy cập để tạo tài khoản.');
        navigate('/login');
      } else {
        setError(err.response?.data?.error?.message || err.message || 'Đã xảy ra lỗi khi tạo tài khoản.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="bg-white p-4 rounded shadow w-100" style={{ maxWidth: '400px' }}>
        <h2 className="text-center mb-4">Đăng ký</h2>
        {error && <div className="alert alert-danger">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label htmlFor="employeeId" className="form-label">Mã nhân viên</label>
            <input
              type="number"
              id="employeeId"
              name="employeeId"
              value={formData.employeeId}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập mã nhân viên"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="username" className="form-label">Tên đăng nhập</label>
            <input
              type="text"
              id="username"
              name="username"
              value={formData.username}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập tên đăng nhập (chữ, số, dấu gạch dưới)"
              required
              disabled={loading}
              maxLength={50}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="password" className="form-label">Mật khẩu</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập mật khẩu (ít nhất 8 ký tự, có chữ và số)"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Vai trò</label>
            {availableRoles.map((role) => (
              <div key={role.roleId} className="form-check">
                <input
                  type="checkbox"
                  id={`role-${role.roleId}`}
                  value={role.roleId}
                  checked={formData.roleIds.includes(role.roleId)}
                  onChange={handleRoleChange}
                  className="form-check-input"
                  disabled={loading}
                />
                <label htmlFor={`role-${role.roleId}`} className="form-check-label">
                  {role.name}
                </label>
              </div>
            ))}
          </div>
          <button type="submit" className="btn btn-primary w-100" disabled={loading}>
            {loading ? 'Đang đăng ký...' : 'Đăng ký'}
          </button>
          <p className="text-center text-muted mt-3">
            Đã có tài khoản? <a href="/" className="text-primary">Đăng nhập</a>
          </p>
        </form>
      </div>
    </div>
  );
}

export default Register;