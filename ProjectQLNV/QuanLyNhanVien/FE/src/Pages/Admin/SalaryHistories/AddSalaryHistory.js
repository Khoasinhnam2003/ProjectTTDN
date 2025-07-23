import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { commandApi } from '../../../api';

function AddSalaryHistory() {
  const [formData, setFormData] = useState({
    employeeId: '',
    salary: '',
    effectiveDate: ''
  });
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      setError('Vui lòng đăng nhập để tiếp tục.');
      navigate('/');
      return;
    }
    // Kiểm tra vai trò Admin (giả định user data có roles)
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
    if (!formData.employeeId || !formData.salary || !formData.effectiveDate) {
      setError('Vui lòng điền đầy đủ tất cả các trường.');
      return;
    }
    setLoading(true);
    try {
      const payload = {
        employeeId: parseInt(formData.employeeId),
        salary: parseFloat(formData.salary),
        effectiveDate: new Date(formData.effectiveDate).toISOString()
      };
      console.log('Sending POST request with payload:', JSON.stringify(payload, null, 2));
      const response = await commandApi.post('api/Salary', payload, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Add SalaryHistory Response:', JSON.stringify(response.data, null, 2));
      if (response.status === 200) {
        alert(`Thêm lịch sử lương với ID ${response.data.salaryHistoryId} thành công!`);
        navigate('/salary-histories');
      } else {
        throw new Error(response.data?.Message || 'Thêm lịch sử lương thất bại.');
      }
    } catch (err) {
      console.error('Add SalaryHistory Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
      if (err.response?.status === 400) {
        setError(err.response.data?.Message || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.');
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else {
        setError(err.response?.data?.Message || err.message || 'Đã xảy ra lỗi khi thêm lịch sử lương.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <h2>Thêm lịch sử lương</h2>
      <button
        className="btn btn-secondary mb-3"
        onClick={() => navigate('/salary-histories')}
        disabled={loading}
      >
        Quay lại
      </button>
      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      <form onSubmit={handleSubmit}>
        <div className="mb-3">
          <label className="form-label">Mã nhân viên</label>
          <input
            type="number"
            className="form-control"
            name="employeeId"
            value={formData.employeeId}
            onChange={handleChange}
            required
            disabled={loading}
            placeholder="Nhập mã nhân viên"
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Lương (VND)</label>
          <input
            type="number"
            step="0.01"
            className="form-control"
            name="salary"
            value={formData.salary}
            onChange={handleChange}
            required
            disabled={loading}
            placeholder="Nhập số tiền lương"
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Ngày hiệu lực</label>
          <input
            type="date"
            className="form-control"
            name="effectiveDate"
            value={formData.effectiveDate}
            onChange={handleChange}
            required
            disabled={loading}
            max={new Date().toISOString().split('T')[0]} // Không cho phép chọn ngày trong tương lai
          />
        </div>
        <button
          type="submit"
          className="btn btn-primary"
          disabled={loading}
        >
          {loading ? 'Đang thêm...' : 'Thêm lịch sử lương'}
        </button>
      </form>
    </div>
  );
}

export default AddSalaryHistory;