import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function AccountDashboard() {
  const [user, setUser] = useState(null);
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [userId, setUserId] = useState('');
  const [searchName, setSearchName] = useState('');
  const [sortConfig, setSortConfig] = useState({ key: null, direction: 'asc' });
  const navigate = useNavigate();
  const location = useLocation();
  const token = localStorage.getItem('token');

  const getActivePage = () => {
    switch (location.pathname) {
      case '/': return 'home';
      case '/admin-dashboard': return 'employees';
      case '/departments': return 'departments';
      case '/positions': return 'positions';
      case '/attendances': return 'attendances';
      case '/contracts': return 'contracts';
      case '/salary-histories': return 'salaryHistories';
      case '/skills': return 'skills';
      case '/account-management': return 'accountManagement';
      default: return 'accountManagement';
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
        console.log('Parsed User Roles:', roleArray);
        setUser({ ...parsedUser, roles: roleArray });
      } catch (err) {
        console.error('Lỗi khi parse dữ liệu user:', err);
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      navigate('/');
    }
  }, [navigate, token]);

  const fetchUsers = async () => {
    try {
      let response;
      setLoading(true);
      setError(null);
      console.log('Fetching users with viewMode:', viewMode, 'userId:', userId, 'searchName:', searchName);
      if (viewMode === 'all') {
        response = await queryApi.get('api/Users', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 }
        });
      } else if (viewMode === 'byId' && userId && parseInt(userId) > 0) {
        response = await queryApi.get(`api/Users/${userId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        response = { data: response.data ? [response.data] : [] };
      } else if (viewMode === 'byName' && searchName) {
        response = await queryApi.get('api/Users/search', {
          headers: { Authorization: `Bearer ${token}` },
          params: { name: searchName, PageNumber: 1, PageSize: 100 }
        });
      } else {
        response = { data: [] };
      }
      console.log('Raw API Response:', response);
      // Xử lý cấu trúc JSON với $values
      const userData = response.data.$values
        ? response.data.$values
        : Array.isArray(response.data)
        ? response.data
        : response.data
        ? [response.data]
        : [];
      console.log('Processed User Data:', userData);
      console.log('Full User Objects:', JSON.stringify(userData, null, 2));
      // Chuẩn hóa dữ liệu
      const normalizedUsers = userData.map(user => ({
        ...user,
        userId: user.userId || user.UserId || null,
        username: user.username || user.Username || 'Chưa có',
        employeeId: user.employeeId || user.EmployeeId || null,
        employeeName: user.employeeName || user.EmployeeName || 'Chưa có',
        roles: user.roles && user.roles.$values ? user.roles.$values : user.roles || []
      }));
      console.log('Normalized User IDs:', normalizedUsers.map(u => u.userId));
      setUsers(normalizedUsers);
      setLoading(false);
    } catch (err) {
      console.error('API Users Error:', err.response ? { status: err.response.status, data: err.response.data } : err.message);
      setError('Không thể tải danh sách tài khoản. Vui lòng thử lại.');
      setLoading(false);
      if (err.response?.status === 404) {
        setUsers([]);
        setError('Tài khoản không tồn tại.');
      } else if (err.response?.status === 400) {
        setError('Tên tìm kiếm không hợp lệ.');
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      }
    }
  };

  useEffect(() => {
    console.log('useEffect triggered with token:', token, 'user:', user, 'viewMode:', viewMode);
    if (token) {
      if (!user) {
        console.log('User not yet loaded, waiting...');
        setLoading(false);
      } else if (user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byId' && userId) || (viewMode === 'byName' && searchName))) {
        fetchUsers();
      } else {
        console.log('No Admin role or invalid viewMode/userId/searchName');
        setLoading(false);
      }
    } else {
      navigate('/');
    }
  }, [token, user, viewMode, userId, searchName, navigate]);

  const handleRefreshUsers = () => {
    setViewMode('all');
    setUserId('');
    setSearchName('');
    setError(null);
    setSortConfig({ key: null, direction: 'asc' });
  };

  const handleAddAccount = () => {
    navigate('/add-account');
  };

  const handleEdit = (userId) => {
    console.log('Attempting to edit userId:', userId);
    if (userId && !isNaN(parseInt(userId))) {
      navigate(`/update-account/${userId}`);
    } else {
      console.error('Invalid userId:', userId);
      setError('ID tài khoản không hợp lệ.');
    }
  };

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

  const handleDelete = async (userId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa tài khoản này?')) {
      try {
        const response = await commandApi.delete(`api/Users/${userId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Delete Response:', response);
        if (response.data && response.data.isSuccess) {
          await fetchUsers();
          alert('Tài khoản đã được xóa thành công!');
        } else {
          throw new Error(response.data.error?.message || 'Xóa tài khoản thất bại.');
        }
      } catch (err) {
        console.error('API Delete User Error:', err.response ? { status: err.response.status, data: err.response.data } : err.message);
        setError('Không thể xóa tài khoản. Vui lòng thử lại.');
      }
    }
  };

  const handleSort = (key) => {
    let direction = 'asc';
    if (sortConfig.key === key && sortConfig.direction === 'asc') {
      direction = 'desc';
    }
    setSortConfig({ key, direction });

    const sortedUsers = [...users].sort((a, b) => {
      let aValue = a[key] || '';
      let bValue = b[key] || '';
      if (key === 'roles') {
        aValue = a.roles?.map(r => r.roleName).join(', ') || '';
        bValue = b.roles?.map(r => r.roleName).join(', ') || '';
      }
      if (typeof aValue === 'string') {
        aValue = aValue.toLowerCase();
        bValue = bValue.toLowerCase();
      }
      if (aValue < bValue) return direction === 'asc' ? -1 : 1;
      if (aValue > bValue) return direction === 'asc' ? 1 : -1;
      return 0;
    });
    setUsers(sortedUsers);
  };

  const getSortIndicator = (key) => {
    if (sortConfig.key === key) {
      return sortConfig.direction === 'asc' ? ' ↑' : ' ↓';
    }
    return '';
  };

  const navButtons = [
    { path: '/admin-dashboard', label: 'Danh sách nhân viên', page: 'employees' },
    { path: '/departments', label: 'Danh sách phòng ban', page: 'departments' },
    { path: '/positions', label: 'Danh sách vị trí', page: 'positions' },
    { path: '/attendances', label: 'Danh sách chấm công', page: 'attendances' },
    { path: '/contracts', label: 'Danh sách hợp đồng', page: 'contracts' },
    { path: '/salary-histories', label: 'Lịch sử lương', page: 'salaryHistories' },
    { path: '/skills', label: 'Danh sách kỹ năng', page: 'skills' },
    { action: handleRefreshUsers, label: 'Quản lý tài khoản', page: 'accountManagement' },
  ];

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Quản lý tài khoản</h2>
        <div>
          <button className="btn btn-success me-2" onClick={handleAddAccount}>
            Thêm tài khoản
          </button>
          <button
            className="btn btn-danger"
            onClick={handleLogout}
            disabled={loading}
          >
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
        {navButtons.map((button) => (
          <button
            key={button.page}
            className={`btn ${getActivePage() === button.page ? 'btn-primary' : 'btn-info'}`}
            onClick={button.action || (() => navigate(button.path))}
            style={{ minWidth: '150px', minHeight: '38px', order: navButtons.indexOf(button) + 1 }}
          >
            {button.label}
          </button>
        ))}
      </div>

      <div className="mb-4">
        <select className="form-select mb-2" value={viewMode} onChange={(e) => setViewMode(e.target.value)}>
          <option value="all">Xem tất cả</option>
          <option value="byId">Xem theo ID</option>
          <option value="byName">Tìm theo tên nhân viên</option>
        </select>
        {viewMode === 'byId' && (
          <input
            type="number"
            className="form-control mb-2"
            placeholder="Nhập User ID"
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
          />
        )}
        {viewMode === 'byName' && (
          <input
            type="text"
            className="form-control mb-2"
            placeholder="Nhập tên nhân viên"
            value={searchName}
            onChange={(e) => setSearchName(e.target.value)}
          />
        )}
        <button className="btn btn-primary" onClick={handleRefreshUsers}>Tải lại</button>
      </div>

      {loading && (
        <div className="text-center">
          <div className="spinner-border" role="status">
            <span className="visually-hidden">Đang tải...</span>
          </div>
        </div>
      )}
      {error && <div className="alert alert-danger">{error}</div>}
      {!loading && !error && (
        <div>
          <p>Tổng số tài khoản hiện có: {users.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered table-hover">
              <thead className="thead-dark" style={{ backgroundColor: '#343a40', color: 'white' }}>
                <tr>
                  <th style={{ cursor: 'pointer' }} onClick={() => handleSort('userId')}>
                    Mã tài khoản {getSortIndicator('userId')}
                  </th>
                  <th style={{ cursor: 'pointer' }} onClick={() => handleSort('username')}>
                    Tên đăng nhập {getSortIndicator('username')}
                  </th>
                  <th style={{ cursor: 'pointer' }} onClick={() => handleSort('employeeId')}>
                    Mã nhân viên {getSortIndicator('employeeId')}
                  </th>
                  <th style={{ cursor: 'pointer' }} onClick={() => handleSort('employeeName')}>
                    Tên nhân viên {getSortIndicator('employeeName')}
                  </th>
                  <th style={{ cursor: 'pointer' }} onClick={() => handleSort('roles')}>
                    Vai trò {getSortIndicator('roles')}
                  </th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {users.length > 0 ? (
                  users.map((user, index) => (
                    <tr key={user.userId || `user-${index}`}>
                      <td>{user.userId || 'N/A'}</td>
                      <td>{user.username || 'Chưa có'}</td>
                      <td>{user.employeeId || 'Chưa có'}</td>
                      <td>{user.employeeName || 'Chưa có'}</td>
                      <td>{user.roles?.length > 0 ? user.roles.map(r => r.roleName).join(', ') : 'Chưa có'}</td>
                      <td>
                        <button
                          className="btn btn-warning btn-sm me-2"
                          onClick={() => handleEdit(user.userId)}
                          disabled={!user.userId}
                        >
                          Chỉnh sửa
                        </button>
                        <button
                          className="btn btn-danger btn-sm"
                          onClick={() => handleDelete(user.userId)}
                          disabled={!user.userId}
                        >
                          Xóa
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center">
                      Không có tài khoản nào.
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

export default AccountDashboard;