import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function AddAccount() {
  const [employees, setEmployees] = useState([]);
  const [roles, setRoles] = useState([]);
  const [formData, setFormData] = useState({
    employeeId: '',
    username: '',
    password: '',
    roleIds: []
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      navigate('/');
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);

        // Lấy danh sách nhân viên
        const employeeResponse = await queryApi.get('api/Employees', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 }
        });
        const employeeData = employeeResponse.data.$values
          ? employeeResponse.data.$values
          : Array.isArray(employeeResponse.data)
          ? employeeResponse.data
          : [];
        setEmployees(employeeData);

        // Lấy danh sách vai trò
        const roleResponse = await queryApi.get('api/Roles', {
          headers: { Authorization: `Bearer ${token}` }
        });
        const roleData = roleResponse.data.$values
          ? roleResponse.data.$values
          : Array.isArray(roleResponse.data)
          ? roleResponse.data
          : [];
        setRoles(roleData);

        setLoading(false);
      } catch (err) {
        console.error('API Error:', err.response ? { status: err.response.status, data: err.response.data } : err.message);
        setError('Không thể tải dữ liệu. Vui lòng thử lại.');
        setLoading(false);
        if (err.response?.status === 401 || err.response?.status === 403) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      }
    };

    fetchData();
  }, [navigate, token]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleRoleChange = (roleId) => {
    const newRoleIds = formData.roleIds.includes(roleId)
      ? formData.roleIds.filter(id => id !== roleId)
      : [...formData.roleIds, roleId];
    setFormData({ ...formData, roleIds: newRoleIds });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      setError(null);
      const response = await commandApi.post('api/Users', {
        employeeId: parseInt(formData.employeeId),
        username: formData.username,
        password: formData.password,
        roleIds: formData.roleIds
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });

      if (response.data && response.data.isSuccess) {
        alert('Tài khoản đã được tạo thành công!');
        navigate('/account-management');
      } else {
        throw new Error(response.data.error?.message || 'Tạo tài khoản thất bại.');
      }
    } catch (err) {
      console.error('API Create User Error:', err.response ? { status: err.response.status, data: err.response.data } : err.message);
      setError(err.response?.data?.error?.message || 'Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin.');
    }
  };

  return (
    <div className="container mt-5">
      <h2>Thêm tài khoản mới</h2>
      {loading && (
        <div className="text-center">
          <div className="spinner-border" role="status">
            <span className="visually-hidden">Đang tải...</span>
          </div>
        </div>
      )}
      {error && <div className="alert alert-danger">{error}</div>}
      {!loading && (
        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label htmlFor="employeeId" className="form-label">Nhân viên</label>
            <select
              id="employeeId"
              name="employeeId"
              className="form-select"
              value={formData.employeeId}
              onChange={handleInputChange}
              required
            >
              <option value="">Chọn nhân viên</option>
              {employees.map(employee => (
                <option key={employee.employeeId} value={employee.employeeId}>
                  {employee.firstName} {employee.lastName} (ID: {employee.employeeId})
                </option>
              ))}
            </select>
          </div>
          <div className="mb-3">
            <label htmlFor="username" className="form-label">Tên đăng nhập</label>
            <input
              type="text"
              id="username"
              name="username"
              className="form-control"
              value={formData.username}
              onChange={handleInputChange}
              required
              pattern="^[a-zA-Z0-9_]+$"
              title="Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới."
            />
          </div>
          <div className="mb-3">
            <label htmlFor="password" className="form-label">Mật khẩu</label>
            <input
              type="password"
              id="password"
              name="password"
              className="form-control"
              value={formData.password}
              onChange={handleInputChange}
              required
              minLength="8"
              title="Mật khẩu phải có ít nhất 8 ký tự, chứa ít nhất một chữ cái và một số."
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Vai trò</label>
            {roles.map(role => (
              <div key={role.roleId} className="form-check">
                <input
                  type="checkbox"
                  className="form-check-input"
                  id={`role-${role.roleId}`}
                  value={role.roleId}
                  checked={formData.roleIds.includes(role.roleId)}
                  onChange={() => handleRoleChange(role.roleId)}
                />
                <label className="form-check-label" htmlFor={`role-${role.roleId}`}>
                  {role.roleName}
                </label>
              </div>
            ))}
          </div>
          <button type="submit" className="btn btn-primary me-2">Thêm tài khoản</button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate('/account-management')}
          >
            Hủy
          </button>
        </form>
      )}
    </div>
  );
}

export default AddAccount;