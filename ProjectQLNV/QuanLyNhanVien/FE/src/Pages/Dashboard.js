import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api';

function Dashboard() {
  const [user, setUser] = useState(null);
  const [employees, setEmployees] = useState([]);
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
  const fetchEmployees = async () => {
    try {
      const response = await api.get('api/Employees', {
        headers: { Authorization: `Bearer ${token}` },
        params: { PageNumber: 1, PageSize: 100 }
      });
      console.log('API Employees Response:', response.data);
      // Lấy mảng từ $values
      const employeeData = response.data.$values || [];
      setEmployees(employeeData);
      setLoading(false);
    } catch (err) {
      console.error('API Employees Error:', err.response ? err.response.data : err.message);
      setError('Không thể tải danh sách nhân viên. Vui lòng thử lại.');
      setLoading(false);
      if (err.response?.status === 401 || err.response?.status === 403) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      }
    }
  };

  if (token && user?.roles.includes('Admin')) {
    fetchEmployees();
  }
}, [token, user, navigate]);

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Danh sách nhân viên</h2>
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
              {employees.length > 0 ? (
                employees.map((employee) => (
                  <tr key={employee.employeeId}>
                    <td>{employee.employeeId}</td>
                    <td>{employee.fullName}</td>
                    <td>{employee.email}</td>
                    <td>{employee.departmentName || 'Chưa có'}</td>
                    <td>{employee.positionName || 'Chưa có'}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="5" className="text-center">
                    Không có nhân viên nào.
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

export default Dashboard;