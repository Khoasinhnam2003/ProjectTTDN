import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function SalaryHistoriesManager() {
  const [user, setUser] = useState(null);
  const [salaryHistories, setSalaryHistories] = useState([]);
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
    const fetchSalaryHistories = async () => {
      if (!user || !user.roles.includes('Manager')) return;
      try {
        setLoading(true);

        // Lấy danh sách nhân viên có role User
        const employeesResponse = await queryApi.get('api/Employees/by-role/User', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 },
        });
        const employeesData = employeesResponse.data.values?.['$values'] || [];
        if (!Array.isArray(employeesData)) {
          throw new Error('Employee data is not an array.');
        }
        const userEmployeeIds = employeesData.map(emp => emp.employeeId);

        // Lấy tất cả lịch sử lương
        const salaryResponse = await queryApi.get('api/Salary', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 },
        });

        console.log('Raw API Response:', JSON.stringify(salaryResponse.data, null, 2));
        const salaryData = salaryResponse.data.$values || [];

        if (!Array.isArray(salaryData)) {
          throw new Error('Salary history data is not an array.');
        }

        console.log('Processed Salary Data:', JSON.stringify(salaryData, null, 2));
        const formattedSalaries = salaryData
          .filter(salary => userEmployeeIds.includes(salary.employeeId))
          .map((salary, index) => ({
            salaryHistoryId: salary.salaryHistoryId || `fallback-${index}`,
            employeeId: salary.employeeId || 'N/A',
            fullName: salary.employeeName || 'Not available',
            salaryAmount: typeof salary.salary === 'number' ? `${salary.salary.toLocaleString('vi-VN').replace(/,/g, '.')}` : 'Not available',
            effectiveDate: salary.effectiveDate ? new Date(salary.effectiveDate).toLocaleDateString('vi-VN') : 'Not available',
          }));

        console.log('Formatted Salaries:', JSON.stringify(formattedSalaries, null, 2));
        setSalaryHistories(formattedSalaries);
        setLoading(false);
      } catch (err) {
        console.error('API Error:', err.response ? err.response.data : err.message);
        if (err.response?.status === 403) {
          setError('You do not have permission to view this information. Please contact Admin.');
        } else if (err.response?.status === 404) {
          setSalaryHistories([]);
          setError('No salary histories found.');
        } else {
          setError(err.response?.data?.Message || 'Unable to load salary history information. Please try again.');
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
      fetchSalaryHistories();
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

  const handleDelete = async (salaryHistoryId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa lịch sử lương này?')) {
      try {
        console.log(`Deleting salary history ID ${salaryHistoryId} with commandApi...`);
        const response = await commandApi.delete(`api/Salary/${salaryHistoryId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Delete Response:', JSON.stringify(response.data, null, 2));
        if (response.status === 200) {
          setSalaryHistories(salaryHistories.filter(sh => sh.salaryHistoryId !== salaryHistoryId));
          alert(`Lịch sử lương với ID ${salaryHistoryId} đã được xóa thành công!`);
        } else {
          throw new Error(response.data?.Message || 'Xóa lịch sử lương thất bại.');
        }
      } catch (err) {
        console.error('Delete Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setError(`Lịch sử lương với ID ${salaryHistoryId} không tồn tại.`);
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || err.message || 'Đã xảy ra lỗi khi xóa lịch sử lương.');
        }
      }
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold">Lịch sử lương (User)</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={() => navigate('/add-salary-history-role-manager')}
          >
            Thêm lịch sử lương
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
        <p className="mb-4 text-muted">
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
        <div>
          <p>Tổng số lịch sử lương hiện có: {salaryHistories.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Salary History ID</th>
                  <th>Employee ID</th>
                  <th>Full Name</th>
                  <th>Salary Amount</th>
                  <th>Effective Date</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {salaryHistories.length > 0 ? (
                  salaryHistories.map((salary) => (
                    <tr key={salary.salaryHistoryId}>
                      <td>{salary.salaryHistoryId}</td>
                      <td>{salary.employeeId}</td>
                      <td>{salary.fullName}</td>
                      <td>{salary.salaryAmount}</td>
                      <td>{salary.effectiveDate}</td>
                      <td>
                        <button
                          className="btn btn-warning me-2"
                          onClick={() => navigate(`/update-salary-history-role-manager/${salary.salaryHistoryId}`)}
                        >
                          Chỉnh sửa
                        </button>
                        <button
                          className="btn btn-danger"
                          onClick={() => handleDelete(salary.salaryHistoryId)}
                        >
                          Xóa
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center">
                      Không có lịch sử lương nào cho nhân viên có vai trò User.
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

export default SalaryHistoriesManager;