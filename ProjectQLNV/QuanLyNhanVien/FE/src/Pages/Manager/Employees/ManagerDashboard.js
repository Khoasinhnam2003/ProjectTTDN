import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function ManagerDashboard() {
  const [user, setUser] = useState(null);
  const [userRoleEmployees, setUserRoleEmployees] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
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
        console.log('User Roles:', roleArray); // Debug vai trò người dùng
      } catch (err) {
        console.error('Error parsing user data:', err);
        setError('Invalid user data in storage. Please log in again.');
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      setError('No user data found. Please log in.');
      navigate('/');
    }
  }, [navigate, token]);

  useEffect(() => {
    const fetchEmployees = async () => {
      if (!user || !user.roles.includes('Manager')) return;
      try {
        setLoading(true);

        const employeesResponse = await queryApi.get('api/Employees/by-role/User', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 },
        });

        console.log('API Employees Response:', employeesResponse.data); // Debug phản hồi API
        const employeesData = employeesResponse.data.values?.['$values'] || [];

        if (!Array.isArray(employeesData)) {
          throw new Error('Employee data is not an array.');
        }

        const employeesWithHours = await Promise.all(
          employeesData.map(async (emp) => {
            try {
              const attendanceResponse = await queryApi.get(`api/Attendance/by-employee/${emp.employeeId}`, {
                headers: { Authorization: `Bearer ${token}` },
                params: { PageNumber: 1, PageSize: 100 },
              });

              console.log(`Attendance Response for Employee ${emp.employeeId}:`, attendanceResponse.data); // Debug
              const attendanceData = attendanceResponse.data.$values || attendanceResponse.data;
              if (!Array.isArray(attendanceData)) {
                throw new Error('Attendance data is not an array.');
              }

              const totalHours = await Promise.all(
                attendanceData.map(async (att) => {
                  if (att.checkOutTime) {
                    try {
                      const workHoursResponse = await queryApi.get(`api/Attendance/${att.attendanceId}/work-hours`, {
                        headers: { Authorization: `Bearer ${token}` },
                      });
                      return parseFloat(workHoursResponse.data.workHours) || 0;
                    } catch (err) {
                      console.error(`Error calculating work hours for attendance ${att.attendanceId}:`, err);
                      return 0;
                    }
                  }
                  return 0;
                })
              );

              const total = totalHours.reduce((sum, hours) => sum + hours, 0).toFixed(2);
              return {
                employeeId: emp.employeeId,
                fullName: emp.fullName || 'Not available',
                email: emp.email || 'Not available',
                departmentName: emp.departmentName || 'Not available',
                positionName: emp.positionName || 'Not available',
                totalWorkHours: total,
              };
            } catch (err) {
              console.error(`Error fetching attendance for employee ${emp.employeeId}:`, err);
              return {
                employeeId: emp.employeeId,
                fullName: emp.fullName || 'Not available',
                email: emp.email || 'Not available',
                departmentName: emp.departmentName || 'Not available',
                positionName: emp.positionName || 'Not available',
                totalWorkHours: 'Error',
              };
            }
          })
        );

        setUserRoleEmployees(employeesWithHours);
        setLoading(false);
      } catch (err) {
        console.error('API Error:', err.response ? err.response.data : err.message);
        if (err.response?.status === 403) {
          setError('You do not have permission to view this information. Please contact Admin.');
        } else if (err.response?.status === 404) {
          setUserRoleEmployees([]);
          setError('No employees with User role found.');
        } else {
          setError(err.response?.data?.Message || 'Unable to load employee information. Please try again.');
        }
        setLoading(false);
        if (err.response?.status === 401 || err.response?.status === 403) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      }
    };

    if (token && user?.roles.includes('Manager')) {
      fetchEmployees();
    }
  }, [token, user, navigate]);

  const handleDeleteEmployee = async (employeeId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa nhân viên này?')) {
      return;
    }

    try {
      setLoading(true);
      await commandApi.delete(`api/Employees/${employeeId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      setUserRoleEmployees((prevEmployees) =>
        prevEmployees.filter((emp) => emp.employeeId !== employeeId)
      );
      console.log(`Employee with ID ${employeeId} deleted successfully`);
    } catch (err) {
      console.error('Error deleting employee:', err.response ? err.response.data : err.message);
      setError('Failed to delete employee. Please try again.');
    } finally {
      setLoading(false);
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
        <h2>Danh sách nhân viên (User)</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={() => navigate('/add-employees-role-manager')}
            disabled={loading}
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
        <div className="table-responsive">
          <table className="table table-striped table-bordered">
            <thead className="thead-dark">
              <tr>
                <th>Employee ID</th>
                <th>Full Name</th>
                <th>Email</th>
                <th>Department</th>
                <th>Position</th>
                <th>Total Work Hours</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {userRoleEmployees.length > 0 ? (
                userRoleEmployees.map((emp) => (
                  <tr key={emp.employeeId}>
                    <td>{emp.employeeId}</td>
                    <td>{emp.fullName}</td>
                    <td>{emp.email}</td>
                    <td>{emp.departmentName}</td>
                    <td>{emp.positionName}</td>
                    <td>{emp.totalWorkHours !== 'Error' ? `${emp.totalWorkHours} hours` : 'Error'}</td>
                    <td>
                      <button
                        className="btn btn-primary btn-sm me-2"
                        onClick={() => navigate(`/update-employee-role-manager/${emp.employeeId}`)}
                      >
                        Chỉnh sửa
                      </button>
                      <button
                        className="btn btn-danger btn-sm"
                        onClick={() => handleDeleteEmployee(emp.employeeId)}
                        disabled={loading}
                      >
                        Xóa
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="7" className="text-center">
                    Không có nhân viên nào với vai trò User.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default ManagerDashboard;