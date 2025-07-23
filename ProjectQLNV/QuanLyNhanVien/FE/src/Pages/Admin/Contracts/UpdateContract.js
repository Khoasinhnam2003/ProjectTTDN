import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateContract() {
  const { contractId } = useParams();
  const [formData, setFormData] = useState({
    employeeId: '',
    contractType: '',
    startDate: '',
    endDate: '',
    salary: '',
    status: ''
  });
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    const fetchContract = async () => {
      if (!token) {
        setError('Vui lòng đăng nhập để tiếp tục.');
        navigate('/');
        return;
      }
      try {
        const response = await queryApi.get(`api/Contracts/${contractId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Fetch Contract Response:', JSON.stringify(response.data, null, 2));
        const contract = response.data;
        setFormData({
          employeeId: contract.employeeId || '',
          contractType: contract.contractType || '',
          startDate: contract.startDate ? new Date(contract.startDate).toISOString().split('T')[0] : '',
          endDate: contract.endDate ? new Date(contract.endDate).toISOString().split('T')[0] : '',
          salary: contract.salary || '',
          status: contract.status || ''
        });
        setLoading(false);
      } catch (err) {
        console.error('Fetch Contract Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        setError('Không thể tải thông tin hợp đồng.');
        setLoading(false);
      }
    };
    if (contractId) fetchContract();
  }, [contractId, navigate, token]);

  useEffect(() => {
    if (!token) {
      setError('Vui lòng đăng nhập để tiếp tục.');
      navigate('/');
      return;
    }
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser);
        const roleArray = parsedUser.roles && parsedUser.roles.$values ? parsedUser.roles.$values : parsedUser.roles || [];
        if (!roleArray.includes('Admin')) {
          setError('Bạn không có quyền truy cập trang này.');
          navigate('/');
        }
      } catch (err) {
        console.error('Lỗi khi parse dữ liệu user:', err);
        setError('Dữ liệu người dùng không hợp lệ. Vui lòng đăng nhập lại.');
        navigate('/');
      }
    } else {
      navigate('/');
    }
  }, [navigate, token]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
    setError(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!token) {
      setError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
      navigate('/');
      return;
    }
    if (!formData.employeeId || !formData.contractType || !formData.startDate || !formData.salary) {
      setError('Vui lòng điền đầy đủ các trường bắt buộc.');
      return;
    }
    setLoading(true);
    try {
      const payload = {
        contractId: parseInt(contractId),
        employeeId: parseInt(formData.employeeId),
        contractType: formData.contractType,
        startDate: new Date(formData.startDate).toISOString(),
        endDate: formData.endDate ? new Date(formData.endDate).toISOString() : null,
        salary: parseFloat(formData.salary),
        status: formData.status || null
      };
      console.log('Sending PUT request with payload:', JSON.stringify(payload, null, 2));
      const response = await commandApi.put(`api/Contracts/${contractId}`, payload, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Update Contract Response:', JSON.stringify(response.data, null, 2));
      if (response.status === 200) {
        alert(`Cập nhật hợp đồng với ID ${contractId} thành công!`);
        navigate('/contracts');
      } else {
        throw new Error(response.data?.error?.message || 'Cập nhật hợp đồng thất bại.');
      }
    } catch (err) {
      console.error('Update Contract Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
      if (err.response?.status === 400) {
        setError(err.response.data?.error?.message || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.');
      } else if (err.response?.status === 404) {
        setError('Hợp đồng không tồn tại.');
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else {
        setError(err.response?.data?.error?.message || err.message || 'Đã xảy ra lỗi khi cập nhật hợp đồng.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="bg-white p-4 rounded shadow w-100" style={{ maxWidth: '500px' }}>
        <h2 className="text-center mb-4">Cập nhật hợp đồng</h2>
        {error && <div className="alert alert-danger">{error}</div>}
        {loading ? (
          <div className="text-center">Đang tải...</div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="mb-3">
              <label htmlFor="employeeId" className="form-label">Mã nhân viên</label>
              <input
                type="number"
                id="employeeId"
                name="employeeId"
                value={formData.employeeId}
                onChange={handleChange}
                className="form-control"
                placeholder="Nhập mã nhân viên"
                required
                disabled={loading}
              />
            </div>
            <div className="mb-3">
              <label htmlFor="contractType" className="form-label">Loại hợp đồng</label>
              <input
                type="text"
                id="contractType"
                name="contractType"
                value={formData.contractType}
                onChange={handleChange}
                className="form-control"
                placeholder="Nhập loại hợp đồng"
                required
                disabled={loading}
              />
            </div>
            <div className="mb-3">
              <label htmlFor="startDate" className="form-label">Ngày bắt đầu</label>
              <input
                type="date"
                id="startDate"
                name="startDate"
                value={formData.startDate}
                onChange={handleChange}
                className="form-control"
                required
                disabled={loading}
                max={new Date().toISOString().split('T')[0]} // Không cho phép chọn ngày trong tương lai
              />
            </div>
            <div className="mb-3">
              <label htmlFor="endDate" className="form-label">Ngày kết thúc</label>
              <input
                type="date"
                id="endDate"
                name="endDate"
                value={formData.endDate}
                onChange={handleChange}
                className="form-control"
                disabled={loading}
                min={formData.startDate || undefined} // Ngày kết thúc không nhỏ hơn ngày bắt đầu
              />
            </div>
            <div className="mb-3">
              <label htmlFor="salary" className="form-label">Lương (VND)</label>
              <input
                type="number"
                step="0.01"
                id="salary"
                name="salary"
                value={formData.salary}
                onChange={handleChange}
                className="form-control"
                placeholder="Nhập số tiền lương"
                required
                disabled={loading}
              />
            </div>
            <div className="mb-3">
              <label htmlFor="status" className="form-label">Trạng thái</label>
              <input
                type="text"
                id="status"
                name="status"
                value={formData.status}
                onChange={handleChange}
                className="form-control"
                placeholder="Nhập trạng thái (tùy chọn)"
                disabled={loading}
              />
            </div>
            <button
              type="submit"
              className="btn btn-primary w-100"
              disabled={loading}
            >
              {loading ? 'Đang cập nhật...' : 'Cập nhật hợp đồng'}
            </button>
            <button
              type="button"
              className="btn btn-secondary w-100 mt-2"
              onClick={() => navigate('/contracts')}
              disabled={loading}
            >
              Quay lại
            </button>
          </form>
        )}
      </div>
    </div>
  );
}

export default UpdateContract;