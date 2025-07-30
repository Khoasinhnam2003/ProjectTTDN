import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function AccountManagementManagerDashboard() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [user, setUser] = useState(null);
  const navigate = useNavigate();
  const location = useLocation();
  const token = localStorage.getItem('token');

  const getActivePage = () => {
    switch (location.pathname) {
      case '/': return 'home';
      case '/manager-dashboard': return 'employees';
      case '/attendances-manager': return 'attendances';
      case '/contracts-manager': return 'contracts';
      case '/salary-histories-manager': return 'salaryHistories';
      case '/skills-manager': return 'skills';
      case '/account-management-manager': return 'accountManagement';
      default: return 'manager';
    }
  };

  // Định nghĩa fetchUsersWithRoleUser như một hàm độc lập
  const fetchUsersWithRoleUser = async () => {
    if (!user || !user.roles.includes('Manager')) return;
    try {
      setLoading(true);

      const userRolesResponse = await queryApi.get('api/UserRoles', {
        headers: { Authorization: `Bearer ${token}` },
      });

      console.log('UserRoles Response:', userRolesResponse.data);

      const userRolesData = userRolesResponse.data.$values || userRolesResponse.data;
      if (!Array.isArray(userRolesData)) {
        throw new Error('Dữ liệu UserRoles không phải là mảng hợp lệ.');
      }

      const userIdsWithUserRole = userRolesData
        .filter(ur => ur.roleName === 'User')
        .map(ur => ur.userId);

      if (userIdsWithUserRole.length === 0) {
        setUsers([]);
        setLoading(false);
        return;
      }

      const userPromises = userIdsWithUserRole.map(async (userId) => {
        const userResponse = await queryApi.get(`api/Users/${userId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        console.log(`User Response for UserId ${userId}:`, userResponse.data);
        const userData = userResponse.data;
        return {
          userId: userData.userId,
          username: userData.username,
          employeeId: userData.employeeId || 'Not available',
          employeeName: userData.employeeName || 'Not available',
          role: userData.role || 'User',
        };
      });

      const usersData = await Promise.all(userPromises);
      setUsers(usersData);
      setLoading(false);
    } catch (err) {
      console.error('Lỗi khi tải danh sách tài khoản:', err.response ? err.response.data : err.message);
      if (err.response?.status === 403) {
        setError('Bạn không có quyền xem thông tin này.');
      } else if (err.response?.status === 404) {
        setUsers([]);
        setError('Không tìm thấy tài khoản nào với vai trò User.');
      } else {
        setError(err.response?.data?.Message || 'Không thể tải danh sách tài khoản.');
      }
      setLoading(false);
      if (err.response?.status === 401 || err.response?.status === 403) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      }
    }
  };

  useEffect(() => {
    console.log('Init - Token:', token);
    if (!token) {
      navigate('/');
      return;
    }

    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser);
        const roleArray = parsedUser.roles && parsedUser.roles.$values ? parsedUser.roles.$values : parsedUser.roles || [];
        setUser({ ...parsedUser, roles: roleArray });
        console.log('User Roles:', roleArray);
        if (!roleArray.includes('Manager')) {
          setError('Bạn không có quyền truy cập trang này.');
          navigate('/');
        }
      } catch (err) {
        console.error('Lỗi phân tích dữ liệu người dùng:', err);
        setError('Dữ liệu người dùng không hợp lệ. Vui lòng đăng nhập lại.');
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      setError('Không tìm thấy dữ liệu người dùng. Vui lòng đăng nhập.');
      navigate('/');
    }
  }, [navigate, token]);

  useEffect(() => {
    if (token && user?.roles.includes('Manager')) {
      fetchUsersWithRoleUser();
    }
  }, [token, user, navigate]);

  const handleLogout = async () => {
    if (!token) {
      navigate('/');
      return;
    }
    setLoading(true);
    try {
      console.log('Sending logout request with token:', token);
      const logoutResponse = await commandApi.post('api/Authentication/logout', {}, {
        headers: { Authorization: `Bearer ${token}` },
      });
      console.log('Logout Response:', JSON.stringify(logoutResponse.data, null, 2));

      if (!logoutResponse.data || typeof logoutResponse.data !== 'object') {
        throw new Error('Phản hồi từ server không hợp lệ.');
      }

      if (!logoutResponse.data.isSuccess) {
        throw new Error(logoutResponse.data.error?.message || 'Đăng xuất thất bại.');
      }

      const data = logoutResponse.data.data || logoutResponse.data;
      const checkOutTime = data.CheckOutTime || data.checkOutTime;

      if (checkOutTime) {
        localStorage.setItem('checkOutTime', checkOutTime);
      } else {
        console.warn('No CheckOutTime received from API.');
      }

      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('checkInTime');

      if (checkOutTime) {
        alert(`Đăng xuất thành công! Thời gian check-out: ${new Date(checkOutTime).toLocaleString('vi-VN', {
          timeZone: 'Asia/Ho_Chi_Minh',
          dateStyle: 'short',
          timeStyle: 'medium',
        })}`);
      } else {
        alert('Đăng xuất thành công! Không có thời gian check-out.');
      }

      navigate('/');
    } catch (err) {
      console.error('Logout error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
      setError(err.response?.data?.error?.message || err.message || 'Đăng xuất thất bại. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateUser = async (userId) => {
    setLoading(true);
    try {
      navigate(`/update-account-role-manager/${userId}`);
    } catch (err) {
      console.error('Error navigating to edit page:', err);
      setError('Không thể chuyển đến trang chỉnh sửa. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteUser = async (userId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa tài khoản này?')) {
      return;
    }
    setLoading(true);
    try {
      console.log('Token for delete:', token);
      console.log('Delete Request URL:', `http://localhost:5221/api/Users/${userId}`);
      const response = await commandApi.delete(`api/Users/${userId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      console.log('Delete Response:', JSON.stringify(response.data, null, 2));

      if (response.status === 200 && response.data.isSuccess) {
        await fetchUsersWithRoleUser(); // Gọi hàm để làm mới danh sách
        alert('Xóa tài khoản thành công!');
      } else {
        throw new Error(response.data.error?.message || 'Xóa tài khoản thất bại.');
      }
    } catch (err) {
      console.error('Error deleting user:', {
        status: err.response?.status,
        data: err.response?.data,
        message: err.message,
      });
      setError(`Lỗi khi xóa tài khoản: ${err.response?.data?.error?.message || err.message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Quản lý tài khoản (User)</h2>
        <div>
          <button className="btn btn-danger" onClick={handleLogout} disabled={loading}>
            {loading ? 'Đang đăng xuất...' : 'Đăng xuất'}
          </button>
        </div>
      </div>

      {user && (
        <p className="mb-4">
          Xin chào, {user.username}! Bạn có vai trò: {Array.isArray(user.roles) ? user.roles.join(', ') : 'Không có vai trò'}
        </p>
      )}

      <div className="mb-4 d-flex flex-wrap gap-2" style={{ flexShrink: 0, position: 'relative' }}>
        <button
          className={`btn ${getActivePage() === 'employees' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/manager-dashboard')}
          style={{ minWidth: '150px', minHeight: '38px', order: 1 }}
        >
          Danh sách nhân viên
        </button>
        <button
          className={`btn ${getActivePage() === 'attendances' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/attendances-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 2 }}
        >
          Danh sách chấm công
        </button>
        <button
          className={`btn ${getActivePage() === 'contracts' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/contracts-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 3 }}
        >
          Danh sách hợp đồng
        </button>
        <button
          className={`btn ${getActivePage() === 'salaryHistories' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/salary-histories-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 4 }}
        >
          Lịch sử lương
        </button>
        <button
          className={`btn ${getActivePage() === 'skills' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/skills-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 5 }}
        >
          Danh sách kỹ năng
        </button>
        <button
          className={`btn ${getActivePage() === 'accountManagement' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/account-management-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 6 }}
        >
          Quản lý tài khoản
        </button>
      </div>

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div>
          <p>Tổng số tài khoản hiện có: {users.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Mã tài khoản</th>
                  <th>Tên đăng nhập</th>
                  <th>Mã nhân viên</th>
                  <th>Tên nhân viên</th>
                  <th>Vai trò</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {users.length > 0 ? (
                  users.map((user, index) => (
                    <tr key={user.userId}>
                      <td>{index + 1}</td>
                      <td>{user.username}</td>
                      <td>{user.employeeId}</td>
                      <td>{user.employeeName}</td>
                      <td>{user.role}</td>
                      <td>
                        <button
                          className="btn btn-warning me-2"
                          onClick={() => handleUpdateUser(user.userId)}
                          disabled={loading}
                        >
                          Chỉnh sửa
                        </button>
                        <button
                          className="btn btn-danger"
                          onClick={() => handleDeleteUser(user.userId)}
                          disabled={loading}
                        >
                          Xóa
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center">
                      Không có tài khoản nào với vai trò User.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

export default AccountManagementManagerDashboard;