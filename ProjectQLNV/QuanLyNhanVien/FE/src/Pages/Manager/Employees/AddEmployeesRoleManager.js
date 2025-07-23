import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { commandApi } from '../../../api';

function AddEmployeesRoleManager() {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    dateOfBirth: '',
    hireDate: '',
    departmentId: '',
    positionId: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      navigate('/');
      return;
    }

    // Kiểm tra vai trò từ localStorage
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser);
        const roleArray = parsedUser.roles && parsedUser.roles.$values ? parsedUser.roles.$values : parsedUser.roles || [];
        if (!roleArray.includes('Manager')) {
          setError('You do not have permission to add employees. Only Managers are allowed.');
          setLoading(false);
          return;
        }
      } catch (err) {
        console.error('Error parsing user data:', err);
        setError('Invalid user data in storage. Please log in again.');
        localStorage.removeItem('user');
        navigate('/');
        return;
      }
    } else {
      setError('No user data found. Please log in.');
      navigate('/');
      return;
    }
  }, [token, navigate]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const payload = {
        ...formData,
        dateOfBirth: formData.dateOfBirth || null,
        hireDate: formData.hireDate || new Date().toISOString().split('T')[0],
        departmentId: formData.departmentId ? parseInt(formData.departmentId, 10) : null,
        positionId: formData.positionId ? parseInt(formData.positionId, 10) : null,
      };

      const response = await commandApi.post('api/Employees', payload, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.data && response.data.isSuccess) {
        alert('Nhân viên đã được tạo thành công!');
        navigate('/admin-dashboard');
      } else {
        throw new Error(response.data.error?.message || 'Tạo nhân viên thất bại.');
      }
    } catch (err) {
      setError(err.message || 'Đã xảy ra lỗi khi tạo nhân viên.');
      console.error('Lỗi chi tiết:', err.response ? err.response.data : err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="bg-white p-4 rounded shadow w-100" style={{ maxWidth: '500px' }}>
        <h2 className="text-center mb-4">Thêm nhân viên</h2>
        {error && <div className="alert alert-danger">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label htmlFor="firstName" className="form-label">Họ</label>
            <input
              type="text"
              id="firstName"
              name="firstName"
              value={formData.firstName}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập họ"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="lastName" className="form-label">Tên</label>
            <input
              type="text"
              id="lastName"
              name="lastName"
              value={formData.lastName}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập tên"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="email" className="form-label">Email</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập email"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="phone" className="form-label">Số điện thoại</label>
            <input
              type="text"
              id="phone"
              name="phone"
              value={formData.phone}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập số điện thoại"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="dateOfBirth" className="form-label">Ngày sinh</label>
            <input
              type="date"
              id="dateOfBirth"
              name="dateOfBirth"
              value={formData.dateOfBirth}
              onChange={handleChange}
              className="form-control"
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="hireDate" className="form-label">Ngày nhận việc</label>
            <input
              type="date"
              id="hireDate"
              name="hireDate"
              value={formData.hireDate}
              onChange={handleChange}
              className="form-control"
              required
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="departmentId" className="form-label">Department ID</label>
            <input
              type="number"
              id="departmentId"
              name="departmentId"
              value={formData.departmentId}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập Department ID"
              disabled={loading}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="positionId" className="form-label">Position ID</label>
            <input
              type="number"
              id="positionId"
              name="positionId"
              value={formData.positionId}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập Position ID"
              disabled={loading}
            />
          </div>
          <button type="submit" className="btn btn-primary w-100" disabled={loading}>
            {loading ? 'Đang tạo...' : 'Tạo nhân viên'}
          </button>
          <button
            type="button"
            className="btn btn-secondary w-100 mt-2"
            onClick={() => navigate('/manager-dashboard')}
            disabled={loading}
          >
            Quay lại
          </button>
        </form>
      </div>
    </div>
  );
}

export default AddEmployeesRoleManager;