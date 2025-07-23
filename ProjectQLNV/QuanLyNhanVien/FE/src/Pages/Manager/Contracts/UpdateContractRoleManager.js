import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateContractRoleManager() {
  const [user, setUser] = useState(null);
  const [contract, setContract] = useState(null);
  const [userEmployeeIds, setUserEmployeeIds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [formData, setFormData] = useState({
    contractType: '',
    startDate: '',
    endDate: '',
    salary: '',
    status: ''
  });
  const navigate = useNavigate();
  const { contractId } = useParams();
  const token = localStorage.getItem('token');

  // Kiểm tra token và quyền Manager
  useEffect(() => {
    console.log('Checking token and user...');
    console.log('Token:', token);
    if (!token) {
      setError('Không tìm thấy token. Vui lòng đăng nhập lại.');
      console.error('No token found. Redirecting to login.');
      navigate('/');
      return;
    }
    const storedUser = localStorage.getItem('user');
    console.log('Stored user:', storedUser);
    if (storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser);
        const roleArray = parsedUser.roles && parsedUser.roles.$values ? parsedUser.roles.$values : parsedUser.roles || [];
        console.log('Parsed user roles:', roleArray);
        setUser({ ...parsedUser, roles: roleArray });
        if (!roleArray.includes('Manager')) {
          setError('Bạn không có quyền truy cập trang này.');
          console.error('User is not a Manager. Redirecting to /contracts-manager.');
          navigate('/contracts-manager');
        }
      } catch (err) {
        console.error('Lỗi khi parse dữ liệu user:', err);
        setError('Dữ liệu người dùng không hợp lệ. Vui lòng đăng nhập lại.');
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      setError('Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.');
      console.error('No user data found in localStorage. Redirecting to login.');
      navigate('/');
    }
  }, [navigate, token]);

  // Lấy danh sách employeeId của nhân viên có vai trò User
  useEffect(() => {
    const fetchUsers = async () => {
      try {
        console.log('Fetching employees with User role...');
        const response = await queryApi.get('api/Employees/by-role/User', {
          headers: { Authorization: `Bearer ${token}` },
          params: { pageNumber: 1, pageSize: 10000 }
        });
        console.log('API Employees/by-role/User Response:', JSON.stringify(response.data, null, 2));
        const employeeIds = response.data.values?.$values?.map(employee => employee.employeeId) || [];
        setUserEmployeeIds([...new Set(employeeIds)]);
      } catch (err) {
        console.error('API Employees/by-role/User Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập danh sách nhân viên. Vui lòng đăng nhập lại.');
          console.error('Unauthorized or Forbidden error in fetchUsers. Redirecting to login.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError('Không thể tải danh sách nhân viên có vai trò User.');
          console.error('Error fetching users, but not redirecting:', err.message);
        }
      }
    };

    if (token && user?.roles.includes('Manager')) {
      fetchUsers();
    }
  }, [token, user, navigate]);

  // Lấy thông tin hợp đồng
  useEffect(() => {
    const fetchContract = async () => {
      try {
        setLoading(true);
        console.log(`Fetching contract with ID ${contractId}...`);
        const response = await queryApi.get(`api/Contracts/${contractId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('API Contract Response:', JSON.stringify(response.data, null, 2));

        const contractData = response.data;
        if (!contractData || !contractData.employeeId) {
          throw new Error('Dữ liệu hợp đồng không hợp lệ.');
        }

        // Kiểm tra employeeId chỉ khi userEmployeeIds đã được tải
        console.log('userEmployeeIds:', userEmployeeIds);
        if (userEmployeeIds.length > 0 && !userEmployeeIds.includes(contractData.employeeId)) {
          setError('Hợp đồng này thuộc về nhân viên không có vai trò User.');
          console.error('Contract employeeId not in userEmployeeIds. Redirecting to /contracts-manager.');
          navigate('/contracts-manager');
          return;
        }

        setContract(contractData);
        setFormData({
          contractType: contractData.contractType || '',
          startDate: contractData.startDate ? new Date(contractData.startDate).toISOString().split('T')[0] : '',
          endDate: contractData.endDate ? new Date(contractData.endDate).toISOString().split('T')[0] : '',
          salary: contractData.salary || '',
          status: contractData.status || ''
        });
        setError(null);
      } catch (err) {
        console.error('API Contract Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setError('Hợp đồng không tồn tại.');
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          console.error('Unauthorized or Forbidden error in fetchContract. Redirecting to login.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || 'Không thể tải thông tin hợp đồng. Vui lòng thử lại.');
          console.error('Error fetching contract, but not redirecting:', err.message);
        }
      } finally {
        setLoading(false);
      }
    };

    if (token && user?.roles.includes('Manager') && userEmployeeIds.length > 0) {
      fetchContract();
    }
  }, [token, user, contractId, userEmployeeIds, navigate]);

  // Xử lý thay đổi input trong form
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  // Xử lý gửi form cập nhật hợp đồng
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!contract) {
      setError('Không có thông tin hợp đồng để cập nhật.');
      return;
    }
    if (!token) {
      setError('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
      console.error('No token found in handleSubmit. Redirecting to login.');
      navigate('/');
      return;
    }
    try {
      setLoading(true);
      const updateData = {
        contractId: parseInt(contractId),
        employeeId: contract.employeeId,
        contractType: formData.contractType,
        startDate: formData.startDate ? `${formData.startDate}T00:00:00` : null,
        endDate: formData.endDate ? `${formData.endDate}T00:00:00` : null,
        salary: parseFloat(formData.salary),
        status: formData.status
      };
      console.log('Token before sending request:', token);
      console.log('Sending update request with data:', JSON.stringify(updateData, null, 2));
      const response = await commandApi.put(`api/Contracts/${contractId}`, updateData, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Update Response:', JSON.stringify(response.data, null, 2));
      if (response.data && response.data.isSuccess) {
        alert('Cập nhật hợp đồng thành công!');
        navigate('/contracts-manager');
      } else {
        throw new Error(response.data?.Message || 'Cập nhật thất bại.');
      }
    } catch (err) {
      console.error('Update Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
      console.error('Error status:', err.response?.status);
      if (err.response?.status === 400) {
        setError('Dữ liệu gửi đi không hợp lệ. Vui lòng kiểm tra lại.');
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Phiên đăng nhập hết hạn hoặc không có quyền cập nhật. Vui lòng đăng nhập lại.');
        console.error('Unauthorized or Forbidden error in handleSubmit. Redirecting to login.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else if (err.response?.status === 404) {
        setError('Hợp đồng không tồn tại.');
      } else {
        setError(err.response?.data?.Message || 'Cập nhật hợp đồng thất bại. Vui lòng thử lại.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Chỉnh sửa hợp đồng (ID: {contractId})</h2>
        <button
          className="btn btn-secondary"
          onClick={() => navigate(-1)}
          disabled={loading}
        >
          Quay lại
        </button>
      </div>
      {user && (
        <p className="mb-4">
          Xin chào, {user.username}! Bạn có vai trò: {Array.isArray(user.roles) ? user.roles.join(', ') : 'Không có vai trò'}
        </p>
      )}
      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      {!loading && !error && contract && (
        <form onSubmit={handleSubmit} className="card p-4 shadow-sm">
          <div className="mb-3">
            <label className="form-label">Mã nhân viên</label>
            <input
              type="text"
              className="form-control"
              value={contract.employeeId}
              disabled
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Họ và tên</label>
            <input
              type="text"
              className="form-control"
              value={contract.employee ? `${contract.employee.firstName} ${contract.employee.lastName}` : 'Chưa có'}
              disabled
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Loại hợp đồng</label>
            <select
              name="contractType"
              className="form-select"
              value={formData.contractType}
              onChange={handleInputChange}
              required
            >
              <option value="">Chọn loại hợp đồng</option>
              <option value="Hợp Đồng Làm Việc">Hợp Đồng Làm Việc</option>
              <option value="Hợp Đồng Thử Việc">Hợp Đồng Thử Việc</option>
              <option value="Hợp Đồng Thời Vụ">Hợp Đồng Thời Vụ</option>
            </select>
          </div>
          <div className="mb-3">
            <label className="form-label">Ngày bắt đầu</label>
            <input
              type="date"
              name="startDate"
              className="form-control"
              value={formData.startDate}
              onChange={handleInputChange}
              required
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Ngày kết thúc</label>
            <input
              type="date"
              name="endDate"
              className="form-control"
              value={formData.endDate}
              onChange={handleInputChange}
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Lương (VND)</label>
            <input
              type="number"
              name="salary"
              className="form-control"
              value={formData.salary}
              onChange={handleInputChange}
              required
              min="0"
              step="1"
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Trạng thái</label>
            <select
              name="status"
              className="form-select"
              value={formData.status}
              onChange={handleInputChange}
              required
            >
              <option value="">Chọn trạng thái</option>
              <option value="Đang Hoạt Động">Đang Hoạt Động</option>
              <option value="Hết Hạn">Hết Hạn</option>
              <option value="Đã Hủy">Đã Hủy</option>
            </select>
          </div>
          <div className="d-flex gap-2">
            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? 'Đang lưu...' : 'Lưu'}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => navigate('/contracts-manager')}
              disabled={loading}
            >
              Hủy
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

export default UpdateContractRoleManager;