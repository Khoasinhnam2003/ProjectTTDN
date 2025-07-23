import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function AdminDashboard() {
  const [user, setUser] = useState(null);
  const [employees, setEmployees] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [employeeId, setEmployeeId] = useState('');
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

  useEffect(() => {
    const fetchEmployees = async () => {
      try {
        let response;
        setLoading(true);
        if (viewMode === 'all') {
          console.log('Fetching all employees with queryApi...');
          response = await queryApi.get('api/Employees', {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        } else if (viewMode === 'byId' && employeeId) {
          console.log(`Fetching employee by ID ${employeeId} with queryApi...`);
          response = await queryApi.get(`api/Employees/${employeeId}`, {
            headers: { Authorization: `Bearer ${token}` }
          });
        } else if (viewMode === 'byDepartment' && departmentId) {
          console.log(`Fetching employees by department ${departmentId} with queryApi...`);
          response = await queryApi.get(`api/Employees/by-department/${departmentId}`, {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        }
        console.log('API Employees Response:', JSON.stringify(response.data, null, 2));
        let employeeData;
        if (viewMode === 'all' || viewMode === 'byDepartment') {
          employeeData = response.data.$values
            .map(emp => ({
              employeeId: emp.employeeId,
              fullName: emp.fullName || 'Chưa có',
              email: emp.email || 'Chưa có',
              departmentName: emp.departmentName || 'Chưa có',
              positionName: emp.positionName || 'Chưa có'
            }))
            .sort((a, b) => a.employeeId - b.employeeId); // Sort by employeeId in ascending order
        } else if (viewMode === 'byId') {
          const employee = response.data;
          if (employee && (employee.employeeId || employee.$values)) {
            if (employee.$values && Array.isArray(employee.$values) && employee.$values.length > 0) {
              employeeData = employee.$values.map(emp => ({
                employeeId: emp.employeeId,
                fullName: emp.fullName || 'Chưa có',
                email: emp.email || 'Chưa có',
                departmentName: emp.departmentName || 'Chưa có',
                positionName: emp.positionName || 'Chưa có'
              }));
            } else if (employee.employeeId) {
              employeeData = [{
                employeeId: employee.employeeId,
                fullName: employee.fullName || 'Chưa có',
                email: employee.email || 'Chưa có',
                departmentName: employee.departmentName || 'Chưa có',
                positionName: employee.positionName || 'Chưa có'
              }];
            } else {
              employeeData = [];
              setError('Không tìm thấy nhân viên với ID này.');
            }
          } else {
            employeeData = [];
            setError('Không tìm thấy nhân viên với ID này.');
          }
        }
        console.log('Mapped employeeData:', JSON.stringify(employeeData, null, 2));
        setEmployees(employeeData);
        setError(null);
        setLoading(false);
      } catch (err) {
        console.error('API Employees Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 405) {
          setError('Phương thức không được phép. Vui lòng kiểm tra cấu hình API.');
        } else if (err.response?.status === 404) {
          setEmployees([]);
          setError('Không tìm thấy nhân viên.');
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || 'Không thể tải danh sách nhân viên. Vui lòng thử lại.');
        }
        setLoading(false);
      }
    };
    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byId' && employeeId) || (viewMode === 'byDepartment' && departmentId))) {
      fetchEmployees();
    }
  }, [token, user, viewMode, employeeId, departmentId, navigate]);

  const handleDelete = async (employeeId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa nhân viên này?')) {
      try {
        console.log(`Deleting employee ID ${employeeId} with commandApi...`);
        const response = await commandApi.delete(`api/Employees/${employeeId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Delete Response:', JSON.stringify(response.data, null, 2));
        if (response.data && response.data.isSuccess) {
          setEmployees(employees.filter(emp => emp.employeeId !== employeeId));
          alert('Nhân viên đã được xóa thành công!');
        } else {
          throw new Error(response.data.error?.message || 'Xóa nhân viên thất bại.');
        }
      } catch (err) {
        console.error('Delete Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        setError(err.response?.data?.error?.message || err.message || 'Đã xảy ra lỗi khi xóa nhân viên.');
      }
    }
  };

  const handleRefreshEmployees = () => {
    setViewMode('all');
    setEmployeeId('');
    setDepartmentId('');
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
        <h2>Danh sách nhân viên</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={() => navigate('/add-employee')}
          >
            Thêm nhân viên
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
          className={`btn ${getActivePage() === 'employees' ? 'btn-primary' : 'btn-info'}`}
          onClick={handleRefreshEmployees}
          style={{ minWidth: '150px', minHeight: '38px', order: 1 }}
        >
          Danh sách nhân viên
        </button>
        <button
          className={`btn ${getActivePage() === 'departments' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/departments')}
          style={{ minWidth: '150px', minHeight: '38px', order: 2 }}
        >
          Danh sách phòng ban
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
          <option value="byDepartment">Xem theo Department</option>
        </select>
        {viewMode === 'byId' && (
          <input
            type="number"
            className="form-control mb-2"
            placeholder="Nhập Employee ID"
            value={employeeId}
            onChange={(e) => {
              setEmployeeId(e.target.value);
              setError(null);
            }}
          />
        )}
        {viewMode === 'byDepartment' && (
          <input
            type="number"
            className="form-control mb-2"
            placeholder="Nhập Department ID"
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value)}
          />
        )}
        <button className="btn btn-primary" onClick={() => setEmployees([])}>Tải lại</button>
      </div>

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div>
          <p>Tổng số nhân viên hiện có: {employees.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Mã NV</th>
                  <th>Họ và Tên</th>
                  <th>Email</th>
                  <th>Phòng Ban</th>
                  <th>Vị trí</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {employees.length > 0 ? (
                  employees.map((employee) => (
                    <tr key={employee.employeeId}>
                      <td>{employee.employeeId}</td>
                      <td>{employee.fullName || 'Chưa có'}</td>
                      <td>{employee.email || 'Chưa có'}</td>
                      <td>{employee.departmentName || 'Chưa có'}</td>
                      <td>{employee.positionName || 'Chưa có'}</td>
                      <td>
                        <button
                          className="btn btn-warning me-2"
                          onClick={() => navigate(`/update-employee/${employee.employeeId}`)}
                        >
                          Chỉnh sửa
                        </button>
                        <button
                          className="btn btn-danger"
                          onClick={() => handleDelete(employee.employeeId)}
                        >
                          Xóa
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center">
                      Không có nhân viên nào.
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

export default AdminDashboard;