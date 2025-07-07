import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { queryApi, commandApi } from '../api'; // Thêm commandApi

function Dashboard() {
  const [user, setUser] = useState(null);
  const [employees, setEmployees] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [employeeId, setEmployeeId] = useState('');
  const [departmentId, setDepartmentId] = useState('');
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
        let response;
        setLoading(true);
        if (viewMode === 'all') {
          response = await queryApi.get('api/Employees', {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        } else if (viewMode === 'byId' && employeeId) {
          response = await queryApi.get(`api/Employees/${employeeId}`, {
            headers: { Authorization: `Bearer ${token}` }
          });
        } else if (viewMode === 'byDepartment' && departmentId) {
          response = await queryApi.get(`api/Employees/by-department/${departmentId}`, {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        }

        console.log('API Employees Response:', response.data);
        let employeeData;
        if (viewMode === 'all' || viewMode === 'byDepartment') {
          employeeData = response.data.$values || [];
        } else if (viewMode === 'byId') {
          const employee = response.data;
          if (employee && employee.employeeId) {
            employeeData = [{
              employeeId: employee.employeeId,
              fullName: employee.fullName || 'Chưa có',
              email: employee.email || 'Chưa có',
              departmentName: employee.departmentName || 'Chưa có',
              positionName: employee.positionName || 'Chưa có'
            }];
          } else {
            employeeData = [];
          }
        }
        setEmployees(employeeData);
        setLoading(false);
      } catch (err) {
        console.error('API Employees Error:', err.response ? err.response.data : err.message);
        setError('Không thể tải danh sách nhân viên. Vui lòng thử lại.');
        setLoading(false);
        if (err.response?.status === 404) {
          setEmployees([]);
          setError(null);
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      }
    };

    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byId' && employeeId) || (viewMode === 'byDepartment' && departmentId))) {
      fetchEmployees();
    }
  }, [token, user, viewMode, employeeId, departmentId, navigate]);

  const handleDelete = async (employeeId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa nhân viên này?')) {
      try {
        const response = await commandApi.delete(`api/Employees/${employeeId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (response.data && response.data.isSuccess) {
          setEmployees(employees.filter(emp => emp.employeeId !== employeeId));
          alert('Nhân viên đã được xóa thành công!');
        } else {
          throw new Error(response.data.error?.message || 'Xóa nhân viên thất bại.');
        }
      } catch (err) {
        setError(err.message || 'Đã xảy ra lỗi khi xóa nhân viên.');
        console.error('Lỗi chi tiết:', err.response ? err.response.data : err);
      }
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
            onClick={() => {
              localStorage.removeItem('token');
              localStorage.removeItem('user');
              navigate('/');
            }}
          >
            Đăng xuất
          </button>
        </div>
      </div>

      {user && (
        <p className="mb-4">
          Xin chào, {user.username}! Bạn có vai trò: {Array.isArray(user.roles) ? user.roles.join(', ') : 'Không có vai trò'}
        </p>
      )}

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
            onChange={(e) => setEmployeeId(e.target.value)}
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
        <div className="table-responsive">
          <table className="table table-striped table-bordered">
            <thead className="thead-dark">
              <tr>
                <th>Mã NV</th>
                <th>Họ và Tên</th>
                <th>Email</th>
                <th>Phòng Ban</th>
                <th>Vị trí</th>
                <th>Hành động</th> {/* Thêm cột Hành động */}
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
      )}
    </div>
  );
}

export default Dashboard;