import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateEmployee() {
  const { employeeId } = useParams();
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    dateOfBirth: '',
    hireDate: '',
    departmentId: '',
    positionId: '',
    isActive: true,
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    const fetchEmployee = async () => {
      try {
        const response = await queryApi.get(`api/Employees/${employeeId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const employee = response.data;
        setFormData({
          firstName: employee.firstName || '',
          lastName: employee.lastName || '',
          email: employee.email || '',
          phone: employee.phone || '',
          dateOfBirth: employee.dateOfBirth ? new Date(employee.dateOfBirth).toISOString().split('T')[0] : '',
          hireDate: employee.hireDate ? new Date(employee.hireDate).toISOString().split('T')[0] : '',
          departmentId: employee.departmentId || '',
          positionId: employee.positionId || '',
          isActive: employee.isActive || true,
        });
        setLoading(false);
      } catch (err) {
        setError('Không thể tải thông tin nhân viên.');
        setLoading(false);
        console.error('Lỗi chi tiết:', err.response ? err.response.data : err);
      }
    };

    if (employeeId) fetchEmployee();
  }, [employeeId, token]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const payload = {
        employeeId: parseInt(employeeId, 10),
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        phone: formData.phone,
        dateOfBirth: formData.dateOfBirth || null,
        hireDate: formData.hireDate || new Date().toISOString().split('T')[0],
        departmentId: formData.departmentId ? parseInt(formData.departmentId, 10) : null,
        positionId: formData.positionId ? parseInt(formData.positionId, 10) : null,
        isActive: formData.isActive,
      };

      const response = await commandApi.put(`api/Employees/${employeeId}`, payload);

      if (response.data && response.data.isSuccess) {
        alert('Nhân viên đã được cập nhật thành công!');
        navigate('/admin-dashboard');
      } else {
        throw new Error(response.data.error?.message || 'Cập nhật nhân viên thất bại.');
      }
    } catch (err) {
      setError(err.message || 'Đã xảy ra lỗi khi cập nhật nhân viên.');
      console.error('Lỗi chi tiết:', err.response ? err.response.data : err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="bg-white p-4 rounded shadow w-100" style={{ maxWidth: '500px' }}>
        <h2 className="text-center mb-4">Cập nhật nhân viên</h2>
        {error && <div className="alert alert-danger">{error}</div>}
        {loading ? (
          <div className="text-center">Đang tải...</div>
        ) : (
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
            <div className="mb-3 form-check">
              <input
                type="checkbox"
                id="isActive"
                name="isActive"
                checked={formData.isActive}
                onChange={handleChange}
                className="form-check-input"
                disabled={loading}
              />
              <label htmlFor="isActive" className="form-check-label">Hoạt động</label>
            </div>
            <button type="submit" className="btn btn-primary w-100" disabled={loading}>
              {loading ? 'Đang cập nhật...' : 'Cập nhật'}
            </button>
            <button
              type="button"
              className="btn btn-secondary w-100 mt-2"
              onClick={() => navigate('/admin-dashboard')}
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

export default UpdateEmployee;