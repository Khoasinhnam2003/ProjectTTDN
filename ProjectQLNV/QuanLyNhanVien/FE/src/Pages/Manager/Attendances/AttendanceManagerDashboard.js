import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function AttendanceManagerDashboard() {
  const [user, setUser] = useState(null);
  const [employees, setEmployees] = useState([]);
  const [attendances, setAttendances] = useState([]);
  const [totalWorkHoursByDay, setTotalWorkHoursByDay] = useState(null);
  const [totalWorkHoursByMonth, setTotalWorkHoursByMonth] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [attendanceId, setAttendanceId] = useState('');
  const [employeeId, setEmployeeId] = useState('');
  const [selectedDate, setSelectedDate] = useState('');
  const [selectedMonth, setSelectedMonth] = useState('');
  const navigate = useNavigate();
  const location = useLocation();
  const token = localStorage.getItem('token');

  const getActivePage = () => {
    switch (location.pathname) {
      case '/': return 'home';
      case '/manager-dashboard': return 'employees';
      case "/attendances-manager": return 'attendances';
      case '/contracts-manager': return 'contracts';
      case '/salary-histories-manager': return 'salaryHistories';
      case '/skills-manager': return 'skills';
      case '/account-management-manager': return 'accountManagement';
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
    const fetchData = async () => {
      try {
        setLoading(true);
        // Fetch employees with "User" role
        const employeesResponse = await queryApi.get('api/Employees/by-role/User', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 }
        });
        console.log('Danh sách nhân viên có vai trò User:', JSON.stringify(employeesResponse.data, null, 2));
        
        let employeeData = [];
        const empData = employeesResponse.data;
        if (empData.values && empData.values.$values && Array.isArray(empData.values.$values)) {
          employeeData = empData.values.$values;
        } else if (empData.values && Array.isArray(empData.values)) {
          employeeData = empData.values;
        } else if (empData && Array.isArray(empData)) {
          employeeData = empData;
        } else {
          throw new Error('Dữ liệu nhân viên không đúng định dạng (không phải mảng).');
        }
        
        // Remove duplicates and ensure employeeId is a number
        const uniqueEmployees = [...new Set(employeeData.map(emp => Number(emp.employeeId)))];
        setEmployees(uniqueEmployees);

        // Fetch attendance records based on viewMode
        let response;
        if (viewMode === 'all') {
          console.log('Đang lấy tất cả bản ghi chấm công của User...');
          response = await queryApi.get('api/Attendance', {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        } else if (viewMode === 'byEmployee' && employeeId) {
          console.log(`Đang lấy bản ghi chấm công theo Employee ID ${employeeId} của User...`);
          response = await queryApi.get(`api/Attendance/by-employee/${employeeId}`, {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        }
        console.log('Phản hồi API chấm công:', JSON.stringify(response.data, null, 2));

        let attendanceData = [];
        if (viewMode === 'all' || viewMode === 'byEmployee') {
          const data = response.data.$values || response.data;
          if (!Array.isArray(data)) {
            console.error('Dữ liệu API không phải mảng:', JSON.stringify(data, null, 2));
            throw new Error('Dữ liệu từ API không đúng định dạng (không phải mảng).');
          }

          attendanceData = await Promise.all(
            data.map(async (att) => {
              const attEmployeeId = Number(att.employeeId);
              if (uniqueEmployees.includes(attEmployeeId)) {
                let workHours = 'N/A';
                if (att.checkOutTime) {
                  try {
                    const workHoursResponse = await queryApi.get(`api/Attendance/${att.attendanceId}/work-hours`, {
                      headers: { Authorization: `Bearer ${token}` }
                    });
                    workHours = Number(workHoursResponse.data.workHours).toFixed(2);
                  } catch (err) {
                    console.error(`Lỗi khi tính giờ làm cho attendance ${att.attendanceId}:`, err);
                    workHours = 'Lỗi';
                  }
                }
                return {
                  attendanceId: att.attendanceId,
                  employeeId: attEmployeeId,
                  employeeName: att.employeeName || 'Chưa có',
                  checkInTime: att.checkInTime
                    ? new Date(att.checkInTime).toLocaleString('vi-VN', {
                        timeZone: 'Asia/Ho_Chi_Minh',
                        dateStyle: 'short',
                        timeStyle: 'medium'
                      })
                    : 'Chưa có',
                  checkOutTime: att.checkOutTime
                    ? new Date(att.checkOutTime).toLocaleString('vi-VN', {
                        timeZone: 'Asia/Ho_Chi_Minh',
                        dateStyle: 'short',
                        timeStyle: 'medium'
                      })
                    : 'Chưa có',
                  workHours,
                  status: att.status || 'Chưa có',
                  notes: att.notes || 'Chưa có',
                  date: att.checkInTime
                    ? new Date(att.checkInTime).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })
                    : 'Chưa có',
                  month: att.checkInTime
                    ? new Date(att.checkInTime).toLocaleString('vi-VN', {
                        timeZone: 'Asia/Ho_Chi_Minh',
                        year: 'numeric',
                        month: '2-digit'
                      })
                    : 'Chưa có'
                };
              }
              return null;
            })
          ).then(results => results.filter(att => att !== null));

          // Calculate total work hours by day
          if (viewMode === 'byEmployee' && employeeId && selectedDate && uniqueEmployees.includes(Number(employeeId))) {
            const totalHoursByDay = attendanceData
              .filter(
                att =>
                  att.date === new Date(selectedDate).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })
              )
              .reduce((sum, att) => sum + (parseFloat(att.workHours) || 0), 0)
              .toFixed(2);
            setTotalWorkHoursByDay(totalHoursByDay);
          } else {
            setTotalWorkHoursByDay(null);
          }

          // Calculate total work hours by month
          if (viewMode === 'byEmployee' && employeeId && selectedMonth && uniqueEmployees.includes(Number(employeeId))) {
            const selectedMonthFormatted = new Date(selectedMonth).toLocaleString('vi-VN', {
              timeZone: 'Asia/Ho_Chi_Minh',
              year: 'numeric',
              month: '2-digit'
            });
            const totalHoursByMonth = attendanceData
              .filter(att => att.month === selectedMonthFormatted)
              .reduce((sum, att) => sum + (parseFloat(att.workHours) || 0), 0)
              .toFixed(2);
            setTotalWorkHoursByMonth(totalHoursByMonth);
          } else {
            setTotalWorkHoursByMonth(null);
          }
        }

        console.log('Dữ liệu chấm công đã xử lý:', JSON.stringify(attendanceData, null, 2));
        setAttendances(attendanceData);
        setError(null);
      } catch (err) {
        console.error('Lỗi API:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 405) {
          setError('Phương thức không được phép. Vui lòng kiểm tra cấu hình API.');
        } else if (err.response?.status === 404) {
          setAttendances([]);
          setError('Không tìm thấy bản ghi chấm công.');
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || err.message || 'Không thể tải danh sách chấm công. Vui lòng thử lại.');
        }
      } finally {
        setLoading(false);
      }
    };

    if (token && user?.roles.includes('Manager') && (viewMode === 'all' || (viewMode === 'byEmployee' && employeeId))) {
      fetchData();
    }
  }, [token, user, viewMode, employeeId, selectedDate, selectedMonth, navigate]);

  const handleLogout = async () => {
    if (!token) {
      navigate('/');
      return;
    }
    setLoading(true);
    try {
      console.log('Đang gửi yêu cầu đăng xuất với token:', token);
      const logoutResponse = await commandApi.post('api/Authentication/logout', {}, {
        headers: { Authorization: `Bearer ${token}` },
      });
      console.log('Phản hồi đăng xuất:', JSON.stringify(logoutResponse.data, null, 2));

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
        console.warn('Không nhận được CheckOutTime từ API.');
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
      console.error('Lỗi đăng xuất:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
      setError(err.response?.data?.error?.message || err.message || 'Đăng xuất thất bại. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  const handleRefreshAttendances = () => {
    setViewMode('all');
    setAttendanceId('');
    setEmployeeId('');
    setSelectedDate('');
    setSelectedMonth('');
    setTotalWorkHoursByDay(null);
    setTotalWorkHoursByMonth(null);
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Danh sách chấm công (User)</h2>
        <div>
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

      <div className="mb-4">
        <select className="form-select mb-2" value={viewMode} onChange={(e) => setViewMode(e.target.value)}>
          <option value="all">Xem tất cả User</option>
          <option value="byEmployee">Xem theo Employee ID</option>
        </select>
        {viewMode === 'byEmployee' && (
          <>
            <input
              type="number"
              className="form-control mb-2"
              placeholder="Nhập Employee ID"
              value={employeeId}
              onChange={(e) => {
                setEmployeeId(e.target.value);
                setError(null);
                setTotalWorkHoursByDay(null);
                setTotalWorkHoursByMonth(null);
              }}
            />
            <input
              type="date"
              className="form-control mb-2"
              value={selectedDate}
              onChange={(e) => {
                setSelectedDate(e.target.value);
                setError(null);
              }}
            />
            <input
              type="month"
              className="form-control mb-2"
              value={selectedMonth}
              onChange={(e) => {
                setSelectedMonth(e.target.value);
                setError(null);
              }}
            />
          </>
        )}
        <button className="btn btn-primary" onClick={() => setAttendances([])}>Tải lại</button>
      </div>

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div>
          {viewMode === 'byEmployee' && employeeId && selectedDate && totalWorkHoursByDay !== null && employees.includes(Number(employeeId)) && (
            <p>Tổng giờ làm của nhân viên {employeeId} trong ngày {new Date(selectedDate).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}: {totalWorkHoursByDay} giờ</p>
          )}
          {viewMode === 'byEmployee' && employeeId && selectedMonth && totalWorkHoursByMonth !== null && employees.includes(Number(employeeId)) && (
            <p>Tổng giờ làm của nhân viên {employeeId} trong tháng {new Date(selectedMonth).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: 'long' })}: {totalWorkHoursByMonth} giờ</p>
          )}
          <p>Tổng số bản ghi chấm công của User: {attendances.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Mã Chấm Công</th>
                  <th>Mã NV</th>
                  <th>Họ và Tên</th>
                  <th>Thời Gian Check-In</th>
                  <th>Thời Gian Check-Out</th>
                  <th>Số Giờ Làm</th>
                  <th>Trạng Thái</th>
                  <th>Ghi Chú</th>
                </tr>
              </thead>
              <tbody>
                {attendances.length > 0 ? (
                  attendances.map((attendance) => (
                    <tr key={attendance.attendanceId}>
                      <td>{attendance.attendanceId}</td>
                      <td>{attendance.employeeId}</td>
                      <td>{attendance.employeeName}</td>
                      <td>{attendance.checkInTime}</td>
                      <td>{attendance.checkOutTime}</td>
                      <td>{attendance.workHours}</td>
                      <td>{attendance.status}</td>
                      <td>{attendance.notes}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="8" className="text-center">
                      Không có bản ghi chấm công nào của User.
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

export default AttendanceManagerDashboard;