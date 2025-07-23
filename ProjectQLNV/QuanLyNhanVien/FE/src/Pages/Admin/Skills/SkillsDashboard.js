import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function SkillsDashboard() {
  const [user, setUser] = useState(null);
  const [skills, setSkills] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [viewMode, setViewMode] = useState('all');
  const [employeeId, setEmployeeId] = useState('');
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
    const fetchSkills = async () => {
      try {
        let response;
        setLoading(true);
        if (viewMode === 'all') {
          console.log('Fetching all skills with queryApi...');
          response = await queryApi.get('api/Skill', {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        } else if (viewMode === 'byEmployee' && employeeId) {
          console.log(`Fetching skills by EmployeeId ${employeeId} with queryApi...`);
          response = await queryApi.get(`api/Skill/by-employee/${employeeId}`, {
            headers: { Authorization: `Bearer ${token}` },
            params: { PageNumber: 1, PageSize: 100 }
          });
        }
        console.log('API Skills Response:', JSON.stringify(response.data, null, 2));
        let skillData = [];
        if (response.data && response.data.$values) {
          const idMap = new Map();
          // Duyệt qua toàn bộ response.data để xây dựng idMap
          const collectIds = (data) => {
            if (Array.isArray(data)) {
              data.forEach(item => collectIds(item));
            } else if (data && typeof data === 'object') {
              if (data.$id && data.skillId) {
                idMap.set(data.$id, data);
              }
              Object.values(data).forEach(collectIds);
            }
          };
          collectIds(response.data);

          // Thu thập tất cả kỹ năng từ $values
          skillData = response.data.$values
            .map(item => {
              if (item.$ref) {
                return idMap.get(item.$ref) || item;
              }
              return item;
            })
            .filter(item => item.skillId !== undefined && item.skillId !== null)
            .map(s => ({
              skillId: s.skillId,
              employeeId: s.employeeId,
              employeeName: s.employeeName || 'Chưa có',
              skillName: s.skillName || 'Chưa có',
              proficiencyLevel: s.proficiencyLevel || 'Chưa có'
            }))
            .sort((a, b) => a.skillId - b.skillId);
        } else if (viewMode === 'byEmployee' && (!response.data || response.data.$values?.length === 0)) {
          setError('Không tìm thấy kỹ năng cho nhân viên này.');
          skillData = [];
        }
        console.log('Mapped skillData:', JSON.stringify(skillData, null, 2));
        setSkills(skillData);
        setError(null);
        setLoading(false);
      } catch (err) {
        console.error('API Skills Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setSkills([]);
          setError('Không tìm thấy kỹ năng.');
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || 'Không thể tải danh sách kỹ năng. Vui lòng thử lại.');
        }
        setLoading(false);
      }
    };
    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byEmployee' && employeeId))) {
      fetchSkills();
    }
  }, [token, user, viewMode, employeeId, navigate]);

  const handleDelete = async (skillId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa kỹ năng này?')) {
      try {
        console.log(`Deleting skill ID ${skillId} with commandApi...`);
        const response = await commandApi.delete(`api/Skills/${skillId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Delete Response:', JSON.stringify(response.data, null, 2));
        if (response.data && response.data.isSuccess) {
          setSkills(skills.filter(s => s.skillId !== skillId));
          alert(`Kỹ năng với ID ${skillId} đã được xóa thành công!`);
        } else {
          throw new Error(response.data.error?.message || 'Xóa kỹ năng thất bại.');
        }
      } catch (err) {
        console.error('Delete Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setError(`Kỹ năng với ID ${skillId} không tồn tại.`);
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.error?.message || err.message || 'Đã xảy ra lỗi khi xóa kỹ năng.');
        }
      }
    }
  };

  const handleRefresh = () => {
    setViewMode('all');
    setEmployeeId('');
    setSkills([]);
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
        <h2>Danh sách kỹ năng</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={() => navigate('/add-skill')}
          >
            Thêm kỹ năng
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
          className={`btn ${getActivePage() === 'employees' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/admin-dashboard')}
          style={{ minWidth: '150px', minHeight: '38px' }}
        >
          Danh sách nhân viên
        </button>
        <button
          className={`btn ${getActivePage() === 'departments' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/departments')}
          style={{ minWidth: '150px', minHeight: '38px' }}
        >
          Danh sách phòng ban
        </button>
        <button
          className={`btn ${getActivePage() === 'positions' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/positions')}
          style={{ minWidth: '150px', minHeight: '38px' }}
        >
          Danh sách vị trí
        </button>
        <button
          className={`btn ${getActivePage() === 'attendances' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/attendances')}
          style={{ minWidth: '150px', minHeight: '38px' }}
        >
          Danh sách chấm công
        </button>
        <button
          className={`btn ${getActivePage() === 'contracts' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/contracts')}
          style={{ minWidth: '150px', minHeight: '38px' }}
        >
          Danh sách hợp đồng
        </button>
        <button
          className={`btn ${getActivePage() === 'salaryHistories' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/salary-histories')}
          style={{ minWidth: '150px', minHeight: '38px' }}
        >
          Lịch sử lương
        </button>
        <button
          className={`btn ${getActivePage() === 'skills' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/skills')}
          style={{ minWidth: '150px', minHeight: '38px' }}
        >
          Danh sách kỹ năng
        </button>
        <button
          className={`btn ${getActivePage() === 'accountManagement' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/account-management')}
          style={{ minWidth: '150px', minHeight: '38px' }}
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
          <p>Tổng số kỹ năng hiện có: {skills.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Mã kỹ năng</th>
                  <th>Mã nhân viên</th>
                  <th>Họ và tên</th>
                  <th>Tên kỹ năng</th>
                  <th>Mức độ thành thạo</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {skills.length > 0 ? (
                  skills.map((s) => (
                    <tr key={s.skillId.toString()}>
                      <td>{s.skillId}</td>
                      <td>{s.employeeId}</td>
                      <td>{s.employeeName}</td>
                      <td>{s.skillName}</td>
                      <td>{s.proficiencyLevel}</td>
                      <td>
                        <button
                          className="btn btn-warning me-2"
                          onClick={() => navigate(`/update-skill/${s.skillId}`)}
                        >
                          Chỉnh sửa
                        </button>
                        <button
                          className="btn btn-danger"
                          onClick={() => handleDelete(s.skillId)}
                        >
                          Xóa
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center">
                      Không có kỹ năng nào.
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

export default SkillsDashboard;