import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function AttendanceDashboard() {
  const [user, setUser] = useState(null);
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
    const fetchAttendances = async () => {
      try {
        let response;
        setLoading(true);
        if (viewMode === 'all') {
          console.log('Đang lấy tất cả bản ghi chấm công...');
          response = await queryApi.get('api/Attendance', {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        } else if (viewMode === 'byEmployee' && employeeId) {
          console.log(`Đang lấy bản ghi chấm công theo Employee ID ${employeeId}...`);
          response = await queryApi.get(`api/Attendance/by-employee/${employeeId}`, {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        }
        console.log('Phản hồi API:', JSON.stringify(response.data, null, 2));
        let attendanceData = [];

        if (viewMode === 'all' || viewMode === 'byEmployee') {
          const data = response.data.$values || response.data;
          if (!Array.isArray(data)) {
            console.error('Dữ liệu API không phải mảng:', data);
            throw new Error('Dữ liệu từ API không đúng định dạng (không phải mảng).');
          }
          attendanceData = await Promise.all(data.map(async (att) => {
            let workHours = 'N/A';
            if (att.checkOutTime) {
              try {
                const workHoursResponse = await queryApi.get(`api/Attendance/${att.attendanceId}/work-hours`, {
                  headers: { Authorization: `Bearer ${token}` }
                });
                workHours = workHoursResponse.data.workHours.toFixed(2);
              } catch (err) {
                console.error(`Lỗi khi tính giờ làm cho attendance ${att.attendanceId}:`, err);
                workHours = 'Lỗi';
              }
            }
            return {
              attendanceId: att.attendanceId,
              employeeId: att.employeeId,
              employeeName: att.employeeName || 'Chưa có',
              checkInTime: att.checkInTime ? new Date(att.checkInTime).toLocaleString('vi-VN', {
                timeZone: 'Asia/Ho_Chi_Minh',
                dateStyle: 'short',
                timeStyle: 'medium'
              }) : 'Chưa có',
              checkOutTime: att.checkOutTime ? new Date(att.checkOutTime).toLocaleString('vi-VN', {
                timeZone: 'Asia/Ho_Chi_Minh',
                dateStyle: 'short',
                timeStyle: 'medium'
              }) : 'Chưa có',
              workHours,
              status: att.status || 'Chưa có',
              notes: att.notes || 'Chưa có',
              date: att.checkInTime ? new Date(att.checkInTime).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : 'Chưa có',
              month: att.checkInTime ? new Date(att.checkInTime).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit' }) : 'Chưa có'
            };
          }));

          // Tính tổng giờ làm theo ngày nếu ở chế độ byEmployee và có ngày được chọn
          if (viewMode === 'byEmployee' && employeeId && selectedDate) {
            const totalHoursByDay = attendanceData
              .filter(att => att.date === new Date(selectedDate).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }))
              .reduce((sum, att) => sum + (parseFloat(att.workHours) || 0), 0)
              .toFixed(2);
            setTotalWorkHoursByDay(totalHoursByDay);
          } else {
            setTotalWorkHoursByDay(null);
          }

          // Tính tổng giờ làm theo tháng nếu ở chế độ byEmployee và có tháng được chọn
          if (viewMode === 'byEmployee' && employeeId && selectedMonth) {
            const selectedMonthFormatted = new Date(selectedMonth).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit' });
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
        setLoading(false);
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
        setLoading(false);
      }
    };

    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byEmployee' && employeeId))) {
      fetchAttendances();
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
        <h2>Danh sách chấm công</h2>
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
          onClick={() => navigate('/admin-dashboard')}
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
          onClick={handleRefreshAttendances}
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
          {viewMode === 'byEmployee' && employeeId && selectedDate && totalWorkHoursByDay !== null && (
            <p>Tổng giờ làm của nhân viên {employeeId} trong ngày {new Date(selectedDate).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}: {totalWorkHoursByDay} giờ</p>
          )}
          {viewMode === 'byEmployee' && employeeId && selectedMonth && totalWorkHoursByMonth !== null && (
            <p>Tổng giờ làm của nhân viên {employeeId} trong tháng {new Date(selectedMonth).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: 'long' })}: {totalWorkHoursByMonth} giờ</p>
          )}
          <p>Tổng số bản ghi chấm công: {attendances.length}</p>
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
                      Không có bản ghi chấm công nào.
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

export default AttendanceDashboard;