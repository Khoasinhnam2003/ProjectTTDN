import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateAccountRoleManager() {
  const { userId } = useParams();
  const [user, setUser] = useState(null);
  const [roles, setRoles] = useState([]);
  const [formData, setFormData] = useState({
    userId: 0,
    username: '',
    password: '',
    roleIds: [],
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    console.log('useParams userId:', userId);
    if (!token) {
      console.log('No token found, redirecting to login');
      navigate('/');
      return;
    }

    const parsedUserId = parseInt(userId);
    if (!userId || isNaN(parsedUserId)) {
      console.error('Invalid userId:', userId);
      setError('ID tài khoản không hợp lệ. Vui lòng kiểm tra URL hoặc thử lại.');
      setLoading(false);
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);

        const [userResponse, roleResponse] = await Promise.all([
          queryApi.get(`api/Users/${parsedUserId}`, {
            headers: { Authorization: `Bearer ${token}` },
          }),
          queryApi.get('api/Roles', {
            headers: { Authorization: `Bearer ${token}` },
          }),
        ]);

        const userData = userResponse.data;
        if (!userData) {
          throw new Error('Tài khoản không tồn tại.');
        }
        setUser(userData);
        setFormData({
          userId: userData.userId || parsedUserId,
          username: userData.username || '',
          password: '',
          roleIds: userData.roles && userData.roles.$values ? userData.roles.$values.map(r => r.roleId) : [],
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
        let errorMessage = 'Không thể tải dữ liệu tài khoản. Vui lòng thử lại.';
        if (err.response?.status === 400) errorMessage = 'Yêu cầu không hợp lệ. Vui lòng kiểm tra ID tài khoản.';
        else if (err.response?.status === 401) {
          errorMessage = 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.';
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else if (err.response?.status === 403) {
          errorMessage = 'Bạn không có quyền truy cập.';
          navigate('/account-management-manager');
        } else if (err.response?.status === 404) errorMessage = 'Tài khoản không tồn tại.';
        setError(errorMessage);
        setLoading(false);
      }
    };

    fetchData();
  }, [navigate, token, userId]);

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

      if (!formData.username.match(/^[a-zA-Z0-9_]+$/)) {
        setError('Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới.');
        return;
      }
      if (formData.password && formData.password.length < 8) {
        setError('Mật khẩu phải có ít nhất 8 ký tự.');
        return;
      }

      console.log('Update Request Data:', formData);
      const response = await commandApi.put(`api/Users/${formData.userId}`, {
        userId: formData.userId,
        username: formData.username,
        password: formData.password || null,
        roleIds: formData.roleIds,
      }, {
        headers: { Authorization: `Bearer ${token}` },
      });

      console.log('Update Response:', JSON.stringify(response.data, null, 2));

      if (response.status === 200 && response.data.isSuccess) {
        alert('Cập nhật tài khoản thành công!');
        navigate('/account-management-manager');
      } else {
        throw new Error(response.data.error?.message || 'Cập nhật tài khoản thất bại.');
      }
    } catch (err) {
      console.error('API Update User Error:', err.response ? { status: err.response.status, data: err.response.data } : err.message);
      let errorMessage = 'Không thể cập nhật tài khoản. Vui lòng kiểm tra lại thông tin.';
      if (err.response?.status === 400) {
        errorMessage = err.response.data.error?.message || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.';
      } else if (err.response?.status === 401) {
        errorMessage = 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.';
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else if (err.response?.status === 403) {
        errorMessage = 'Bạn không có quyền cập nhật tài khoản.';
      }
      setError(errorMessage);
    }
  };

  const handleCancel = () => {
    navigate('/account-management-manager');
  };

  return (
    <div className="container mt-5">
      <h2>Chỉnh sửa tài khoản</h2>
      {loading && (
        <div className="text-center">
          <div className="spinner-border" role="status">
            <span className="visually-hidden">Đang tải...</span>
          </div>
        </div>
      )}
      {error && (
        <div className="alert alert-danger">
          {error}
          <button className="btn btn-link" onClick={handleCancel}>Quay lại</button>
        </div>
      )}
      {!loading && !error && (
        <form onSubmit={handleSubmit}>
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
            <label htmlFor="password" className="form-label">Mật khẩu mới (để trống nếu không thay đổi)</label>
            <input
              type="password"
              id="password"
              name="password"
              className="form-control"
              value={formData.password}
              onChange={handleInputChange}
              minLength="8"
              title="Mật khẩu phải có ít nhất 8 ký tự."
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Vai trò</label>
            {roles.length === 0 ? (
              <div className="alert alert-warning">Không có vai trò nào.</div>
            ) : (
              roles.map(role => (
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
              ))
            )}
          </div>
          <button type="submit" className="btn btn-primary me-2" disabled={loading}>
            Cập nhật tài khoản
          </button>
          <button type="button" className="btn btn-secondary" onClick={handleCancel}>
            Hủy
          </button>
        </form>
      )}
    </div>
  );
}

export default UpdateAccountRoleManager;