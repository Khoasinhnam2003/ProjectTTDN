import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { queryApi, commandApi } from '../../api';

function UserDashboard() {
  const [user, setUser] = useState(null);
  const [employee, setEmployee] = useState(null);
  const [totalWorkHours, setTotalWorkHours] = useState(null); // New state for total work hours
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

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
    const fetchEmployeeAndAttendance = async () => {
      if (!user || !user.employeeId) return;
      try {
        setLoading(true);

        // Fetch employee data
        const employeeResponse = await queryApi.get(`api/Employees/${user.employeeId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        console.log('API Employee Response:', employeeResponse.data);
        const employeeData = employeeResponse.data;
        if (employeeData && employeeData.employeeId) {
          setEmployee({
            employeeId: employeeData.employeeId,
            fullName: employeeData.fullName || 'Not available',
            email: employeeData.email || 'Not available',
            departmentName: employeeData.departmentName || 'Not available',
            positionName: employeeData.positionName || 'Not available',
          });
        } else {
          setEmployee(null);
        }

        // Fetch attendance data
        const attendanceResponse = await queryApi.get(`api/Attendance/by-employee/${user.employeeId}`, {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 }, // Adjust PageSize as needed
        });

        console.log('API Attendance Response:', attendanceResponse.data);
        const attendanceData = attendanceResponse.data.$values || attendanceResponse.data;

        if (!Array.isArray(attendanceData)) {
          throw new Error('Attendance data is not an array.');
        }

        // Calculate total work hours
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
        setTotalWorkHours(total);

        setLoading(false);
      } catch (err) {
        console.error('API Error:', err.response ? err.response.data : err.message);
        if (err.response?.status === 403) {
          setError('You do not have permission to view this information. Please contact Admin.');
        } else if (err.response?.status === 404) {
          setEmployee(null);
          setTotalWorkHours(null);
          setError('Employee or attendance information not found.');
        } else {
          setError(err.response?.data?.Message || 'Unable to load information. Please try again.');
        }
        setLoading(false);
        if (err.response?.status === 401 || err.response?.status === 403) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      }
    };

    if (token && user?.roles.includes('User')) {
      fetchEmployeeAndAttendance();
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

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2> Thông tin nhân viên của bạn </h2>
        <button
          className="btn btn-danger"
          onClick={handleLogout}
          disabled={loading}
        >
          {loading ? 'Đang đăng xuất...' : 'Đăng xuất'}
        </button>
      </div>

      {user && (
        <p className="mb-4">
          Hello, {user.username}! Your role: {Array.isArray(user.roles) ? user.roles.join(', ') : 'No role'}
        </p>
      )}

      {loading && <div className="text-center">Loading...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && employee && (
        <div className="card p-4 shadow-sm">
          <div className="mb-3">
            <strong>Employee ID:</strong> {employee.employeeId}
          </div>
          <div className="mb-3">
            <strong>Full Name:</strong> {employee.fullName}
          </div>
          <div className="mb-3">
            <strong>Email:</strong> {employee.email}
          </div>
          <div className="mb-3">
            <strong>Department:</strong> {employee.departmentName}
          </div>
          <div className="mb-3">
            <strong>Position:</strong> {employee.positionName}
          </div>
          <div className="mb-3">
            <strong>Total Work Hours:</strong> {totalWorkHours ? `${totalWorkHours} hours` : 'Not available'}
          </div>
          <button
            className="btn btn-primary w-100"
            onClick={() => navigate(`/edit-employee/${employee.employeeId}`)}
          >
            Chỉnh sửa
          </button>
        </div>
      )}

      {!loading && !error && !employee && (
        <div className="alert alert-warning text-center">
          Employee information not found.
        </div>
      )}
    </div>
  );
}

export default UserDashboard;