import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function DepartmentsDashboard() {
  const [user, setUser] = useState(null);
  const [departments, setDepartments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [departmentId, setDepartmentId] = useState('');
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
      } catch (err) {
        console.error('Lỗi khi parse dữ liệu user:', err);
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      navigate('/');
    }
  }, [navigate, token]);

  const fetchDepartments = async () => {
    try {
      let response;
      setLoading(true);
      if (viewMode === 'all') {
        response = await queryApi.get('api/Departments', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 }
        });
      } else if (viewMode === 'byId' && departmentId && parseInt(departmentId) > 0) {
        console.log('Fetching employee count for departmentId:', departmentId);
        const employeeCountResponse = await queryApi.get(`api/Departments/${departmentId}/employee-count`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        const allResponse = await queryApi.get('api/Departments', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 }
        });
        const allDepartments = allResponse.data.$values || allResponse.data || [];
        const baseDept = allDepartments.find(d => d.DepartmentId === parseInt(departmentId) || d.departmentId === parseInt(departmentId)) || {};
        if (Object.keys(baseDept).length === 0) {
          response = {
            data: {
              $values: [{
                DepartmentId: departmentId,
                DepartmentName: 'Không có',
                Location: 'Không có',
                ManagerName: 'Không có',
                EmployeeCount: 0
              }]
            }
          };
        } else {
          response = {
            data: {
              $values: [{
                DepartmentId: departmentId,
                DepartmentName: baseDept.DepartmentName || baseDept.departmentName || null,
                Location: baseDept.Location || baseDept.location || null,
                ManagerName: baseDept.ManagerName || baseDept.managerName || null,
                EmployeeCount: employeeCountResponse.data.EmployeeCount || employeeCountResponse.data.employeeCount || 0
              }]
            }
          };
        }
      }
      console.log('Raw API Response:', response);
      let departmentData = response?.data?.$values || (Array.isArray(response.data) ? response.data : []);
      if (!departmentData || departmentData.length === 0) {
        departmentData = [{ DepartmentId: 'N/A', DepartmentName: 'Không có dữ liệu', Location: '', ManagerName: '', EmployeeCount: 0 }];
      }
      console.log('Processed Department Data:', departmentData.map(d => ({ DepartmentId: d.DepartmentId, DepartmentName: d.DepartmentName })));
      setDepartments(departmentData);
      setLoading(false);
    } catch (err) {
      console.error('API Departments Error:', err.response ? err.response.data : err.message);
      setError('Không thể tải danh sách phòng ban. Vui lòng thử lại.');
      setLoading(false);
      if (err.response?.status === 404) {
        setDepartments([]);
        setError(null);
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      }
    }
  };

  useEffect(() => {
    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byId' && departmentId))) {
      fetchDepartments();
    }
  }, [token, user, viewMode, departmentId, navigate]);

  const handleRefreshDepartments = () => {
    setViewMode('all');
    setDepartmentId('');
  };

  const handleAddDepartment = () => {
    navigate('/add-department');
  };

  const handleEdit = (departmentId) => {
    console.log('Attempting to edit departmentId:', departmentId);
    if (departmentId && !isNaN(parseInt(departmentId))) {
      navigate(`/update-department/${departmentId}`);
    } else {
      console.error('Invalid departmentId:', departmentId);
      setError('ID phòng ban không hợp lệ.');
    }
  };

  const handleDelete = async (departmentId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa phòng ban này?')) {
      try {
        const response = await commandApi.delete(`api/Departments/${departmentId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (response.data && response.data.isSuccess) {
          await fetchDepartments(); // Refresh departments list immediately
          alert('Phòng ban đã được xóa thành công!');
        } else {
          throw new Error(response.data.error?.message || 'Xóa phòng ban thất bại.');
        }
      } catch (err) {
        console.error('API Delete Department Error:', err.response ? err.response.data : err.message);
        setError('Không thể xóa phòng ban. Vui lòng thử lại.');
      }
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

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Danh sách phòng ban</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={handleAddDepartment}
          >
            Thêm phòng ban
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
        <button
          className={`btn ${getActivePage() === 'departments' ? 'btn-primary' : 'btn-info'}`}
          onClick={handleRefreshDepartments}
          style={{ minWidth: '150px', minHeight: '38px', order: 2 }}
        >
          Danh sách phòng ban
        </button>
        <button
          className={`btn ${getActivePage() === 'employees' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/admin-dashboard')}
          style={{ minWidth: '150px', minHeight: '38px', order: 1 }}
        >
          Danh sách nhân viên
        </button>
        <button
          className={`btn ${getActivePage() === 'positions' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/positions')}
          style={{ minWidth: '150px', minHeight: '38px', order: 3 }}
        >
          Danh sách vị trí
        </button>
        <button
          className={`btn ${getActivePage() === 'attendances' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/attendances')}
          style={{ minWidth: '150px', minHeight: '38px', order: 4 }}
        >
          Danh sách chấm công
        </button>
        <button
          className={`btn ${getActivePage() === 'contracts' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/contracts')}
          style={{ minWidth: '150px', minHeight: '38px', order: 5 }}
        >
          Danh sách hợp đồng
        </button>
        <button
          className={`btn ${getActivePage() === 'salaryHistories' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/salary-histories')}
          style={{ minWidth: '150px', minHeight: '38px', order: 6 }}
        >
          Lịch sử lương
        </button>
        <button
          className={`btn ${getActivePage() === 'skills' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/skills')}
          style={{ minWidth: '150px', minHeight: '38px', order: 7 }}
        >
          Danh sách kỹ năng
        </button>
         <button
          className={`btn ${getActivePage() === 'accountManagement' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/account-management')}
          style={{ minWidth: '150px', minHeight: '38px', order: 8 }}
        >
          Quản lý tài khoản
        </button>
      </div>

      <div className="mb-4">
        <select className="form-select mb-2" value={viewMode} onChange={(e) => setViewMode(e.target.value)}>
          <option value="all">Xem tất cả</option>
          <option value="byId">Xem theo ID</option>
        </select>
        {viewMode === 'byId' && (
          <input
            type="number"
            className="form-control mb-2"
            placeholder="Nhập Department ID"
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value)}
          />
        )}
        <button className="btn btn-primary" onClick={handleRefreshDepartments}>Tải lại</button>
      </div>

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div>
          <p>Tổng số lượng phòng ban hiện có: {departments.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Mã phòng ban</th>
                  <th>Tên phòng ban</th>
                  <th>Vị trí</th>
                  <th>Quản lý</th>
                  <th>Số lượng nhân viên</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {departments.length > 0 ? (
                  departments.map((department) => (
                    <tr key={department.DepartmentId || department.departmentId || Math.random().toString(36).substr(2, 9)}>
                      <td>{department.DepartmentId || department.departmentId || 'N/A'}</td>
                      <td>{department.DepartmentName || department.departmentName || 'Chưa có'}</td>
                      <td>{department.Location || department.location || 'Chưa có'}</td>
                      <td>{department.ManagerName || department.managerName || 'Chưa có'}</td>
                      <td>{department.EmployeeCount || department.employeeCount || 'N/A'}</td>
                      <td>
                        {department.DepartmentName !== 'Không có' && (
                          <>
                            <button
                              className="btn btn-warning me-2"
                              onClick={() => handleEdit(department.DepartmentId || department.departmentId)}
                            >
                              Chỉnh sửa
                            </button>
                            <button
                              className="btn btn-danger"
                              onClick={() => handleDelete(department.DepartmentId || department.departmentId)}
                            >
                              Xóa
                            </button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center">
                      Không có phòng ban nào.
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

export default DepartmentsDashboard;
