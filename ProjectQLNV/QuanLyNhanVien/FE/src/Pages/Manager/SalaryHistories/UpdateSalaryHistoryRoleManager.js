import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateSalaryHistoryRoleManager() {
  const { salaryHistoryId } = useParams();
  const [salary, setSalary] = useState({
    salaryHistoryId: '',
    employeeId: '',
    fullName: '',
    salaryAmount: '',
    effectiveDate: '',
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    const fetchSalaryHistory = async () => {
      if (!token) {
        navigate('/');
        return;
      }
      try {
        setLoading(true);
        const response = await queryApi.get(`api/Salary/${salaryHistoryId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        console.log('Salary Detail Response:', JSON.stringify(response.data, null, 2));
        const data = response.data;
        setSalary({
          salaryHistoryId: data.salaryHistoryId || '',
          employeeId: data.employeeId || '',
          fullName: data.employeeName || '',
          salaryAmount: typeof data.salary === 'number' ? data.salary.toString() : '',
          effectiveDate: data.effectiveDate ? new Date(data.effectiveDate).toISOString().split('T')[0] : '',
        });
        setLoading(false);
      } catch (err) {
        console.error('API Error:', err.response ? err.response.data : err.message);
        if (err.response?.status === 404) {
          setError(`Lịch sử lương với ID ${salaryHistoryId} không tồn tại.`);
        } else if (err.response?.status === 401 || err.response?.status === 403) {
          setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || 'Không thể tải thông tin lịch sử lương. Vui lòng thử lại.');
        }
        setLoading(false);
      }
    };

    if (token) {
      fetchSalaryHistory();
    }
  }, [token, salaryHistoryId, navigate]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setSalary(prev => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!token) {
      navigate('/');
      return;
    }
    setLoading(true);
    try {
      const updatedSalary = {
        salaryHistoryId: salary.salaryHistoryId,
        employeeId: salary.employeeId,
        employeeName: salary.fullName,
        salary: parseFloat(salary.salaryAmount) || 0,
        effectiveDate: salary.effectiveDate,
      };
      const response = await commandApi.put(`api/Salary/${salaryHistoryId}`, updatedSalary, {
        headers: { Authorization: `Bearer ${token}` },
      });
      console.log('Update Response:', JSON.stringify(response.data, null, 2));
      if (response.status === 200) {
        alert(`Cập nhật lịch sử lương với ID ${salaryHistoryId} thành công!`);
        navigate('/salary-histories-manager');
      } else {
        throw new Error(response.data?.Message || 'Cập nhật thất bại.');
      }
    } catch (err) {
      console.error('Update Error:', err.response ? err.response.data : err.message);
      if (err.response?.status === 404) {
        setError(`Lịch sử lương với ID ${salaryHistoryId} không tồn tại.`);
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else {
        setError(err.response?.data?.Message || err.message || 'Đã xảy ra lỗi khi cập nhật.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold">Chỉnh sửa lịch sử lương</h2>
        <button
          className="btn btn-secondary"
          onClick={() => navigate('/salary-histories-manager')}
        >
          Quay lại
        </button>
      </div>

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label">Salary History ID</label>
            <input
              type="text"
              className="form-control"
              name="salaryHistoryId"
              value={salary.salaryHistoryId}
              onChange={handleChange}
              disabled
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Employee ID</label>
            <input
              type="text"
              className="form-control"
              name="employeeId"
              value={salary.employeeId}
              onChange={handleChange}
              required
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Full Name</label>
            <input
              type="text"
              className="form-control"
              name="fullName"
              value={salary.fullName}
              onChange={handleChange}
              required
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Salary Amount</label>
            <input
              type="number"
              className="form-control"
              name="salaryAmount"
              value={salary.salaryAmount}
              onChange={handleChange}
              required
            />
          </div>
          <div className="mb-3">
            <label className="form-label">Effective Date</label>
            <input
              type="date"
              className="form-control"
              name="effectiveDate"
              value={salary.effectiveDate}
              onChange={handleChange}
              required
            />
          </div>
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Đang lưu...' : 'Lưu'}
          </button>
        </form>
      )}
    </div>
  );
}

export default UpdateSalaryHistoryRoleManager;