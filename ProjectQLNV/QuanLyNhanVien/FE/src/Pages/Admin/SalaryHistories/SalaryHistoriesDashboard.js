import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function SalaryHistoriesDashboard() {
  const [user, setUser] = useState(null);
  const [salaryHistories, setSalaryHistories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [employeeId, setEmployeeId] = useState('');
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
        if (!roleArray.includes('Admin')) {
          setError('Bạn không có quyền truy cập trang này.');
          navigate('/');
        }
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
    const fetchSalaryHistories = async () => {
      try {
        let response;
        setLoading(true);
        if (viewMode === 'all') {
          console.log('Fetching all salary histories with queryApi...');
          response = await queryApi.get('api/Salary', {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        } else if (viewMode === 'byEmployee' && employeeId) {
          console.log(`Fetching salary histories by EmployeeId ${employeeId} with queryApi...`);
          response = await queryApi.get(`api/Salary/by-employee/${employeeId}`, {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        }
        console.log('API SalaryHistories Response:', JSON.stringify(response.data, null, 2));
        let salaryHistoryData = [];

        // Xử lý dữ liệu API
        if (Array.isArray(response.data)) {
          salaryHistoryData = response.data;
        } else if (response.data && response.data.$values && Array.isArray(response.data.$values)) {
          salaryHistoryData = response.data.$values;
        } else if (response.data && typeof response.data === 'object' && Object.keys(response.data).length === 0) {
          salaryHistoryData = [];
        } else {
          throw new Error('Dữ liệu trả về từ API không phải là mảng.');
        }

        // Ánh xạ dữ liệu
        salaryHistoryData = salaryHistoryData.map(sh => ({
          salaryHistoryId: sh.salaryHistoryId,
          employeeId: sh.employeeId,
          employeeName: sh.employeeName || 'Chưa có',
          salary: sh.salary || 0,
          effectiveDate: sh.effectiveDate ? new Date(sh.effectiveDate).toLocaleDateString('vi-VN') : 'Chưa có'
        }))
        .sort((a, b) => a.salaryHistoryId - b.salaryHistoryId);

        console.log('Mapped salaryHistoryData:', JSON.stringify(salaryHistoryData, null, 2));
        setSalaryHistories(salaryHistoryData);
        setError(null);
        setLoading(false);
      } catch (err) {
        console.error('API SalaryHistories Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setSalaryHistories([]);
          setError('Không tìm thấy lịch sử lương.');
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || err.message || 'Không thể tải danh sách lịch sử lương. Vui lòng thử lại.');
        }
        setLoading(false);
      }
    };
    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byEmployee' && employeeId))) {
      fetchSalaryHistories();
    }
  }, [token, user, viewMode, employeeId, navigate]);

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

  const handleRefresh = () => {
    setViewMode('all');
    setEmployeeId('');
    setSalaryHistories([]);
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
        headers: { Authorization: `Bearer ${token}` }
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
        alert(`Đăng xuất thành công! Thời gian check-out: ${new Date(checkOutTime).toLocaleString('vi-VN', {
          timeZone: 'Asia/Ho_Chi_Minh',
          dateStyle: 'short',
          timeStyle: 'medium'
        })}`);
      } else {
        alert('Đăng xuất thành công! Không có thời gian check-out.');
      }
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('checkInTime');
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
        <h2>Lịch sử lương</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={() => navigate('/add-salary-history')}
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
        <p className="mb-4">
          Xin chào, {user.username}! Bạn có vai trò: {Array.isArray(user.roles) ? user.roles.join(', ') : 'Không có vai trò'}
        </p>
      )}

      <div className="mb-4 d-flex flex-wrap gap-2">
        <button
          className="btn btn-info"
          onClick={() => navigate('/admin-dashboard')}
        >
          Danh sách nhân viên
        </button>
        <button
          className="btn btn-info"
          onClick={() => navigate('/departments')}
        >
          Danh sách phòng ban
        </button>
        <button
          className="btn btn-info"
          onClick={() => navigate('/positions')}
        >
          Danh sách vị trí
        </button>
        <button
          className="btn btn-info"
          onClick={() => navigate('/attendances')}
        >
          Danh sách chấm công
        </button>
        <button
          className="btn btn-info"
          onClick={() => navigate('/contracts')}
        >
          Danh sách hợp đồng
        </button>
        <button
          className="btn btn-primary"
          onClick={() => navigate('/salary-histories')}
        >
          Lịch sử lương
        </button>
        <button
          className="btn btn-info"
          onClick={() => navigate('/skills')}
        >
          Danh sách kỹ năng
        </button>
        <button
          className="btn btn-info"
          onClick={() => navigate('/account-management')}
        >
          Quản lý tài khoản
        </button>
      </div>

      <div className="mb-4">
        <select
          className="form-select mb-2"
          value={viewMode}
          onChange={(e) => setViewMode(e.target.value)}
        >
          <option value="all">Xem tất cả</option>
          <option value="byEmployee">Xem theo Employee ID</option>
        </select>
        {viewMode === 'byEmployee' && (
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
        <button
          className="btn btn-primary"
          onClick={handleRefresh}
        >
          Tải lại
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
                  <th>Mã lịch sử lương</th>
                  <th>Mã nhân viên</th>
                  <th>Tên nhân viên</th>
                  <th>Lương</th>
                  <th>Ngày hiệu lực</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {salaryHistories.length > 0 ? (
                  salaryHistories.map((sh) => (
                    <tr key={sh.salaryHistoryId}>
                      <td>{sh.salaryHistoryId}</td>
                      <td>{sh.employeeId}</td>
                      <td>{sh.employeeName}</td>
                      <td>{sh.salary.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' })}</td>
                      <td>{sh.effectiveDate}</td>
                      <td>
                        <button
                          className="btn btn-warning me-2"
                          onClick={() => navigate(`/update-salary-history/${sh.salaryHistoryId}`)}
                        >
                          Chỉnh sửa
                        </button>
                        <button
                          className="btn btn-danger"
                          onClick={() => handleDelete(sh.salaryHistoryId)}
                        >
                          Xóa
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center">
                      Không có lịch sử lương nào.
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

export default SalaryHistoriesDashboard;