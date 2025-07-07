import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { queryApi } from '../api';

function UserDashboard() {
  const [user, setUser] = useState(null);
  const [employee, setEmployee] = useState(null); // Chỉ lưu thông tin của nhân viên hiện tại
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
        console.error('Lỗi khi parse dữ liệu user:', err);
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      navigate('/');
    }
  }, [navigate, token]);

  useEffect(() => {
    const fetchEmployee = async () => {
      if (!user || !user.employeeId) return; // Chỉ fetch nếu có employeeId
      try {
        setLoading(true);
        const response = await queryApi.get(`api/Employees/${user.employeeId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        console.log('API Employee Response:', response.data);
        const employeeData = response.data;
        if (employeeData && employeeData.employeeId) {
          setEmployee({
            employeeId: employeeData.employeeId,
            fullName: employeeData.fullName || 'Chưa có',
            email: employeeData.email || 'Chưa có',
            departmentName: employeeData.departmentName || 'Chưa có',
            positionName: employeeData.positionName || 'Chưa có',
          });
        } else {
          setEmployee(null);
        }
        setLoading(false);
      } catch (err) {
        console.error('API Employee Error:', err.response ? err.response.data : err.message);
        setError('Không thể tải thông tin nhân viên. Vui lòng thử lại.');
        setLoading(false);
        if (err.response?.status === 404) {
          setEmployee(null);
          setError(null);
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      }
    };

    if (token && user?.roles.includes('User')) {
      fetchEmployee();
    }
  }, [token, user, navigate]);

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Thông tin nhân viên của bạn</h2>
        <button
          className="btn btn-danger"
          onClick={() => {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            navigate('/');
          }}
        >
          Đăng xuất
        </button>
      </div>

      {user && (
        <p className="mb-4">
          Xin chào, {user.username}! Bạn có vai trò: {Array.isArray(user.roles) ? user.roles.join(', ') : 'Không có vai trò'}
        </p>
      )}

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div className="table-responsive">
          <table className="table table-striped table-bordered">
            <thead className="thead-dark">
              <tr>
                <th>Mã NV</th>
                <th>Họ và Tên</th>
                <th>Email</th>
                <th>Phòng Ban</th>
                <th>Vị trí</th>
              </tr>
            </thead>
            <tbody>
              {employee ? (
                <tr key={employee.employeeId}>
                  <td>{employee.employeeId}</td>
                  <td>{employee.fullName}</td>
                  <td>{employee.email}</td>
                  <td>{employee.departmentName}</td>
                  <td>{employee.positionName}</td>
                </tr>
              ) : (
                <tr>
                  <td colSpan="5" className="text-center">
                    Không tìm thấy thông tin nhân viên.
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

export default UserDashboard;