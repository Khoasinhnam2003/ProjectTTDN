import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function ContractsDashboard() {
  const [user, setUser] = useState(null);
  const [contracts, setContracts] = useState([]);
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
    const fetchContracts = async () => {
      try {
        let response;
        setLoading(true);
        if (viewMode === 'all') {
          console.log('Fetching all contracts with queryApi...');
          response = await queryApi.get('api/Contracts', {
            headers: { Authorization: `Bearer ${token}` },
            params: { pageNumber: 1, pageSize: 100 }
          });
        } else if (viewMode === 'byEmployee' && employeeId) {
          console.log(`Fetching contracts by EmployeeId ${employeeId} with queryApi...`);
          response = await queryApi.get(`api/Contracts/employee/${employeeId}`, {
            headers: { Authorization: `Bearer ${token}` },
            params: { pageNumber: 1, pageSize: 100 }
          });
        }
        console.log('API Contracts Response:', JSON.stringify(response.data, null, 2));
        let contractData = [];
        if (response.data && response.data.$values) {
          const idMap = new Map();
          // Duyệt qua toàn bộ response.data để xây dựng idMap
          const collectIds = (data) => {
            if (Array.isArray(data)) {
              data.forEach(item => collectIds(item));
            } else if (data && typeof data === 'object') {
              if (data.$id && data.contractId) {
                idMap.set(data.$id, data);
              }
              Object.values(data).forEach(collectIds);
            }
          };
          collectIds(response.data);

          // Lấy thông tin employee từ response
          const employeeMap = new Map();
          const collectEmployees = (data) => {
            if (Array.isArray(data)) {
              data.forEach(item => collectEmployees(item));
            } else if (data && typeof data === 'object') {
              if (data.$id && data.employeeId && data.firstName && data.lastName) {
                employeeMap.set(data.employeeId, data);
              }
              Object.values(data).forEach(collectEmployees);
            }
          };
          collectEmployees(response.data);

          // Thu thập tất cả hợp đồng từ $values và employee.contracts.$values
          const allContracts = [
            ...response.data.$values,
            ...(response.data.$values[0]?.employee?.contracts?.$values || [])
          ].filter(item => item.contractId !== undefined && item.contractId !== null);

          // Thay thế $ref bằng đối tượng từ idMap và gắn thông tin employee
          contractData = allContracts.map(item => {
            let contract = item;
            if (item.$ref) {
              contract = idMap.get(item.$ref) || item;
            }
            // Lấy thông tin employee từ employeeMap hoặc contract.employee
            const employee = employeeMap.get(contract.employeeId) || contract.employee || {};
            return {
              contractId: contract.contractId,
              employeeId: contract.employeeId,
              fullName: employee.firstName && employee.lastName 
                ? `${employee.firstName} ${employee.lastName}`.trim() 
                : 'Chưa có',
              contractType: contract.contractType || 'Chưa có',
              startDate: contract.startDate ? new Date(contract.startDate).toLocaleDateString('vi-VN') : 'Chưa có',
              endDate: contract.endDate ? new Date(contract.endDate).toLocaleDateString('vi-VN') : 'Chưa có',
              salary: contract.salary || 0,
              status: contract.status || 'Chưa có'
            };
          });
        }
        contractData = contractData
          .sort((a, b) => a.contractId - b.contractId);
        console.log('Mapped contractData:', JSON.stringify(contractData, null, 2));
        setContracts(contractData);
        setError(null);
        setLoading(false);
      } catch (err) {
        console.error('API Contracts Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setContracts([]);
          setError('Không tìm thấy hợp đồng.');
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || 'Không thể tải danh sách hợp đồng. Vui lòng thử lại.');
        }
        setLoading(false);
      }
    };
    if (token && user?.roles.includes('Admin') && (viewMode === 'all' || (viewMode === 'byEmployee' && employeeId))) {
      fetchContracts();
    }
  }, [token, user, viewMode, employeeId, navigate]);

  const handleDelete = async (contractId) => {
    if (window.confirm('Bạn có chắc chắn muốn xóa hợp đồng này?')) {
      try {
        console.log(`Deleting contract ID ${contractId} with commandApi...`);
        const response = await commandApi.delete(`api/Contracts/${contractId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Delete Response:', JSON.stringify(response.data, null, 2));
        if (response.status === 200) {
          setContracts(contracts.filter(c => c.contractId !== contractId));
          alert(`Hợp đồng với ID ${contractId} đã được xóa thành công!`);
        } else {
          throw new Error(response.data?.Message || 'Xóa hợp đồng thất bại.');
        }
      } catch (err) {
        console.error('Delete Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setError(`Hợp đồng với ID ${contractId} không tồn tại.`);
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || err.message || 'Đã xảy ra lỗi khi xóa hợp đồng.');
        }
      }
    }
  };

  const handleRefresh = () => {
    setViewMode('all');
    setEmployeeId('');
    setContracts([]);
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
        <h2>Danh sách hợp đồng</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={() => navigate('/add-contract')}
          >
            Thêm hợp đồng
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
          <p>Tổng số hợp đồng hiện có: {contracts.length}</p>
          <div className="table-responsive">
            <table className="table table-striped table-bordered">
              <thead className="thead-dark">
                <tr>
                  <th>Mã hợp đồng</th>
                  <th>Mã nhân viên</th>
                  <th>Họ và tên</th>
                  <th>Loại hợp đồng</th>
                  <th>Ngày bắt đầu</th>
                  <th>Ngày kết thúc</th>
                  <th>Lương</th>
                  <th>Trạng thái</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>{contracts.length > 0 ? contracts.map((c) => <tr key={c.contractId.toString()}><td>{c.contractId}</td><td>{c.employeeId}</td><td>{c.fullName}</td><td>{c.contractType}</td><td>{c.startDate}</td><td>{c.endDate}</td><td>{c.salary.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' })}</td><td>{c.status}</td><td><button className="btn btn-warning me-2" onClick={() => navigate(`/update-contract/${c.contractId}`)}>Chỉnh sửa</button><button className="btn btn-danger" onClick={() => handleDelete(c.contractId)}>Xóa</button></td></tr>) : <tr><td colSpan="9" className="text-center">Không có hợp đồng nào.</td></tr>}</tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

export default ContractsDashboard;