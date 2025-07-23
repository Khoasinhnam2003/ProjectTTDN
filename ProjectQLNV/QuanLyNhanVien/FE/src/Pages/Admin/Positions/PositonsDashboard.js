import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function PositionsDashboard() {
  const [user, setUser] = useState(null);
  const [positions, setPositions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [positionId, setPositionId] = useState('');
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
    console.log('Init - Token:', token, 'User:', user);
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
        console.log('Parsed User:', { ...parsedUser, roles: roleArray });
      } catch (err) {
        console.error('Lỗi khi parse dữ liệu user:', err);
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      navigate('/');
    }
  }, [navigate, token]);

  const fetchPositions = async () => {
    try {
      setLoading(true);
      setError(null); // Xóa lỗi trước khi gọi API
      let response;
      console.log('Fetching positions with viewMode:', viewMode, 'positionId:', positionId);
      if (viewMode === 'all') {
        response = await queryApi.get('api/Positions', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 }
        });
        console.log('Raw Response (GetAll):', response);
        let positionData = response?.data?.$values || (Array.isArray(response.data) ? response.data : []);
        if (!positionData || positionData.length === 0) {
          positionData = [{ positionId: 'N/A', positionName: 'Không có dữ liệu', description: '', baseSalary: 0 }];
        }
        setPositions(positionData.map(p => ({
          positionId: p.positionId || p.PositionId,
          positionName: p.positionName || p.PositionName,
          description: p.description || p.Description,
          baseSalary: p.baseSalary || p.BaseSalary
        })));
      } else if (viewMode === 'byId' && positionId && parseInt(positionId) > 0) {
        console.log('Fetching position with positionId:', positionId);
        response = await queryApi.get(`api/Positions/${positionId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Raw Response (GetById):', response);
        const posData = response.data;
        if (!posData || !(posData.positionId || posData.PositionId)) {
          throw new Error('Vị trí không tồn tại.');
        }
        setPositions([{
          positionId: posData.positionId || posData.PositionId,
          positionName: posData.positionName || posData.PositionName,
          description: posData.description || posData.Description,
          baseSalary: posData.baseSalary || posData.BaseSalary
        }]);
      } else {
        setPositions([]);
      }
      setLoading(false);
    } catch (err) {
      console.error('API Positions Error:', err.response ? err.response.data : err.message);
      console.error('Error Status:', err.response ? err.response.status : 'No status');
      let errorMessage = 'Không thể tải danh sách vị trí. Vui lòng thử lại.';
      if (err.response?.status === 404) {
        errorMessage = 'Vị trí không tồn tại.';
        setPositions([]);
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        errorMessage = 'Không có quyền truy cập hoặc phiên đăng nhập hết hạn.';
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else {
        errorMessage = err.response?.data?.message || err.message;
      }
      setError(errorMessage);
      setLoading(false);
    }
  };

  useEffect(() => {
    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byId' && positionId))) {
      fetchPositions();
    } else {
      setLoading(false);
    }
  }, [token, user, viewMode, positionId, navigate]);

  const handleRefreshPositions = () => {
    setViewMode('all');
    setPositionId('');
    setError(null); // Xóa lỗi khi làm mới
  };

  const handleAddPosition = () => {
    navigate('/add-position');
  };

  const handleEdit = (positionId) => {
    console.log('Attempting to edit positionId:', positionId);
    if (positionId && !isNaN(parseInt(positionId))) {
      navigate(`/update-position/${positionId}`);
    } else {
      console.error('Invalid positionId:', positionId);
      setError('ID vị trí không hợp lệ.');
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

  const handleDelete = async (positionId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa vị trí này?')) {
      try {
        const response = await commandApi.delete(`api/Positions/${positionId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Delete Response:', response);
        if (response.data && response.data.isSuccess) {
          await fetchPositions(); // Làm mới danh sách
          alert('Vị trí đã được xóa thành công!');
        } else {
          throw new Error(response.data.error?.message || 'Xóa vị trí thất bại.');
        }
      } catch (err) {
        console.error('API Delete Position Error:', err.response ? err.response.data : err.message);
        setError('Không thể xóa vị trí. Vui lòng thử lại.');
      }
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Danh sách vị trí</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={handleAddPosition}
          >
            Thêm vị trí
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
          onClick={handleRefreshPositions}
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
        </select>
        {viewMode === 'byId' && (
          <input
            type="number"
            className="form-control mb-2"
            placeholder="Nhập Position ID"
            value={positionId}
            onChange={(e) => setPositionId(e.target.value)}
          />
        )}
        <button className="btn btn-primary" onClick={handleRefreshPositions}>Tải lại</button>
      </div>

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      {!loading && !error && (
        <div>
          <p>Tổng số lượng vị trí hiện có: {positions.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Mã vị trí</th>
                  <th>Tên vị trí</th>
                  <th>Mô tả</th>
                  <th>Lương cơ bản</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {positions.length > 0 ? (
                  positions.map((position) => (
                    <tr key={position.positionId || position.PositionId || Math.random().toString(36).substr(2, 9)}>
                      <td>{position.positionId || position.PositionId || 'N/A'}</td>
                      <td>{position.positionName || position.PositionName || 'Chưa có'}</td>
                      <td>{position.description || position.Description || 'Chưa có'}</td>
                      <td>{position.baseSalary || position.BaseSalary || 'N/A'}</td>
                      <td>
                        {position.positionName !== 'Không có dữ liệu' && (
                          <>
                            <button
                              className="btn btn-warning me-2"
                              onClick={() => handleEdit(position.positionId || position.PositionId)}
                            >
                              Chỉnh sửa
                            </button>
                            <button
                              className="btn btn-danger"
                              onClick={() => handleDelete(position.positionId || position.PositionId)}
                            >
                              Xóa
                            </button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="5" className="text-center">
                      Không có vị trí nào.
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

export default PositionsDashboard;