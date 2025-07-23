import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateSalaryHistory() {
  const { salaryHistoryId } = useParams();
  const [formData, setFormData] = useState({
    employeeId: '',
    salary: '',
    effectiveDate: ''
  });
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      navigate('/');
      return;
    }
    const fetchSalaryHistory = async () => {
      try {
        const response = await queryApi.get(`api/Salary/${salaryHistoryId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Fetch SalaryHistory Response:', JSON.stringify(response.data, null, 2));
        const salaryHistory = response.data;
        if (salaryHistory) {
          setFormData({
            employeeId: salaryHistory.employeeId,
            salary: salaryHistory.salary,
            effectiveDate: new Date(salaryHistory.effectiveDate).toISOString().split('T')[0]
          });
          setLoading(false);
        } else {
          setError('Không tìm thấy lịch sử lương.');
          setLoading(false);
        }
      } catch (err) {
        console.error('Fetch SalaryHistory Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
        if (err.response?.status === 404) {
          setError(`Lịch sử lương với ID ${salaryHistoryId} không tồn tại.`);
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || 'Không thể tải thông tin lịch sử lương.');
        }
        setLoading(false);
      }
    };
    fetchSalaryHistory();
  }, [salaryHistoryId, token, navigate]);

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
    if (!formData.salary || !formData.effectiveDate) {
      setError('Vui lòng điền đầy đủ các trường lương và ngày hiệu lực.');
      return;
    }
    setLoading(true);
    try {
      const payload = {
        salaryHistoryId: parseInt(salaryHistoryId),
        salary: parseFloat(formData.salary),
        effectiveDate: new Date(formData.effectiveDate).toISOString()
      };
      console.log('Sending PUT request with payload:', JSON.stringify(payload, null, 2));
      const response = await commandApi.put(`api/Salary/${salaryHistoryId}`, payload, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Update SalaryHistory Response:', JSON.stringify(response.data, null, 2));
      if (response.status === 200) {
        alert(`Cập nhật lịch sử lương với ID ${salaryHistoryId} thành công!`);
        navigate('/salary-histories');
      } else {
        throw new Error(response.data?.Message || 'Cập nhật thất bại.');
      }
    } catch (err) {
      console.error('Update SalaryHistory Error:', err.response ? JSON.stringify(err.response.data, null, 2) : err.message);
      if (err.response?.status === 400) {
        setError(err.response.data?.Message || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.');
      } else if (err.response?.status === 404) {
        setError(`Lịch sử lương với ID ${salaryHistoryId} không tồn tại.`);
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else {
        setError(err.response?.data?.Message || err.message || 'Đã xảy ra lỗi khi cập nhật lịch sử lương.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <h2>Chỉnh sửa lịch sử lương</h2>
      <button
        className="btn btn-secondary mb-3"
        onClick={() => navigate('/salary-histories')}
        disabled={loading}
      >
        Quay lại
      </button>
      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}
      {!loading && !error && (
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
              disabled
              placeholder="Mã nhân viên không thể thay đổi"
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
            {loading ? 'Đang cập nhật...' : 'Cập nhật lịch sử lương'}
          </button>
        </form>
      )}
    </div>
  );
}

export default UpdateSalaryHistory;