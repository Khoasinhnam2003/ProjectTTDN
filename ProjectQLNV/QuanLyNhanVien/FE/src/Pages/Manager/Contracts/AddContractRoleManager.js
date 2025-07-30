import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function AddContractRoleManager() {
  const [user, setUser] = useState(null);
  const [userEmployeeIds, setUserEmployeeIds] = useState([]);
  const [employees, setEmployees] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [formData, setFormData] = useState({
    employeeId: '',
    contractType: '',
    startDate: '',
    endDate: '',
    salary: '',
    status: ''
  });
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  // Check token and Manager role
  useEffect(() => {
    if (!token) {
      setError('Không tìm thấy token. Vui lòng đăng nhập lại.');
      navigate('/');
      return;
    }
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser);
        const roleArray = parsedUser.roles && parsedUser.roles.$values ? parsedUser.roles.$values : parsedUser.roles || [];
        setUser({ ...parsedUser, roles: roleArray });
        if (!roleArray.includes('Manager')) {
          setError('Bạn không có quyền truy cập trang này.');
          navigate('/contracts-manager');
        }
      } catch (err) {
        setError('Dữ liệu người dùng không hợp lệ. Vui lòng đăng nhập lại.');
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      setError('Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.');
      navigate('/');
    }
  }, [navigate, token]);

  // Fetch employee data with User role
  useEffect(() => {
    const fetchUsers = async () => {
      try {
        setLoading(true);
        const response = await queryApi.get('api/Employees/by-role/User', {
          headers: { Authorization: `Bearer ${token}` },
          params: { pageNumber: 1, pageSize: 10000 }
        });
        const employeeData = response.data.values?.$values || [];
        const employeeList = employeeData.map(emp => ({
          employeeId: emp.employeeId
        }));
        setEmployees(employeeList);
        setUserEmployeeIds(employeeList.map(emp => emp.employeeId));
        setError(null);
      } catch (err) {
        if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập danh sách nhân viên.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError('Không thể tải danh sách nhân viên.');
        }
      } finally {
        setLoading(false);
      }
    };

    if (token && user?.roles.includes('Manager')) {
      fetchUsers();
    }
  }, [token, user, navigate]);

  // Handle form input changes
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    setError(null);
  };

  // Handle form submission
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!token) {
      setError('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
      navigate('/');
      return;
    }
    // Client-side validation
    if (!formData.employeeId || !formData.contractType || !formData.startDate || !formData.salary || !formData.status) {
      setError('Vui lòng điền đầy đủ các trường bắt buộc.');
      return;
    }
    const startDate = new Date(formData.startDate);
    const endDate = formData.endDate ? new Date(formData.endDate) : null;
    if (endDate && endDate < startDate) {
      setError('Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.');
      return;
    }
    if (parseFloat(formData.salary) <= 0) {
      setError('Lương phải lớn hơn 0.');
      return;
    }
    if (formData.contractType.length > 50) {
      setError('Loại hợp đồng tối đa 50 ký tự.');
      return;
    }
    if (formData.status.length > 50) {
      setError('Trạng thái tối đa 50 ký tự.');
      return;
    }
    if (!userEmployeeIds.includes(parseInt(formData.employeeId))) {
      setError('Nhân viên được chọn phải có vai trò User.');
      return;
    }

    try {
      setLoading(true);
      const createData = {
        employeeId: parseInt(formData.employeeId),
        contractType: formData.contractType,
        startDate: `${formData.startDate}T00:00:00`,
        endDate: formData.endDate ? `${formData.endDate}T00:00:00` : null,
        salary: parseFloat(formData.salary),
        status: formData.status
      };
      const response = await commandApi.post('api/Contracts', createData, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (response.data && response.data.isSuccess) {
        alert('Thêm hợp đồng thành công!');
        navigate('/contracts-manager');
      } else {
        throw new Error(response.data?.Message || 'Thêm hợp đồng thất bại.');
      }
    } catch (err) {
      if (err.response?.status === 400) {
        setError(err.response.data?.Message || 'Dữ liệu không hợp lệ.');
      } else if (err.response?.status === 401) {
        setError('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else if (err.response?.status === 403) {
        setError('Bạn không có quyền thêm hợp đồng này.');
      } else {
        setError(err.response?.data?.Message || 'Thêm hợp đồng thất bại.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-lg w-full max-w-lg p-6">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-800">Thêm hợp đồng mới</h2>
          <button
            className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 disabled:bg-gray-300"
            onClick={() => navigate('/contracts-manager')}
            disabled={loading}
          >
            Quay lại
          </button>
        </div>
        {user && (
          <p className="mb-4 text-gray-600">
            Xin chào, {user.username}! Vai trò: {Array.isArray(user.roles) ? user.roles.join(', ') : 'Không có vai trò'}
          </p>
        )}
        {loading && <div className="text-center text-gray-600">Đang tải...</div>}
        {error && <div className="bg-red-100 text-red-700 p-3 rounded mb-4">{error}</div>}
        {!loading && !error && (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">Nhân viên (ID)</label>
              <select
                name="employeeId"
                className="mt-1 block w-full border border-gray-300 rounded p-2"
                value={formData.employeeId}
                onChange={handleInputChange}
                required
                disabled={loading}
              >
                <option value="">Chọn ID nhân viên</option>
                {employees.map(emp => (
                  <option key={emp.employeeId} value={emp.employeeId}>
                    {emp.employeeId}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Loại hợp đồng</label>
              <input
                type="text"
                name="contractType"
                className="mt-1 block w-full border border-gray-300 rounded p-2"
                value={formData.contractType}
                onChange={handleInputChange}
                required
                disabled={loading}
                maxLength={50}
                placeholder="Nhập loại hợp đồng"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Ngày bắt đầu</label>
              <input
                type="date"
                name="startDate"
                className="mt-1 block w-full border border-gray-300 rounded p-2"
                value={formData.startDate}
                onChange={handleInputChange}
                required
                disabled={loading}
                max={new Date().toISOString().split('T')[0]}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Ngày kết thúc</label>
              <input
                type="date"
                name="endDate"
                className="mt-1 block w-full border border-gray-300 rounded p-2"
                value={formData.endDate}
                onChange={handleInputChange}
                disabled={loading}
                min={formData.startDate || undefined}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Lương (VND)</label>
              <input
                type="number"
                name="salary"
                className="mt-1 block w-full border border-gray-300 rounded p-2"
                value={formData.salary}
                onChange={handleInputChange}
                required
                disabled={loading}
                min="0"
                step="0.01"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Trạng thái</label>
              <select
                name="status"
                className="mt-1 block w-full border border-gray-300 rounded p-2"
                value={formData.status}
                onChange={handleInputChange}
                required
                disabled={loading}
              >
                <option value="">Chọn trạng thái</option>
                <option value="Đang Hoạt Động">Đang Hoạt Động</option>
                <option value="Hết Hạn">Hết Hạn</option>
                <option value="Đã Hủy">Đã Hủy</option>
              </select>
            </div>
            <div className="flex gap-3">
              <button
                type="submit"
                className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:bg-blue-300"
                disabled={loading}
              >
                {loading ? 'Đang lưu...' : 'Thêm hợp đồng'}
              </button>
              <button
                type="button"
                className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 disabled:bg-gray-300"
                onClick={() => navigate('/contracts-manager')}
                disabled={loading}
              >
                Hủy
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

export default AddContractRoleManager;