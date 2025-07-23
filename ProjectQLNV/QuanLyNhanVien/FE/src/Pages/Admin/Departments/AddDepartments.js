import { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { commandApi } from '../../../api';

function AddDepartments() {
  const [newDepartment, setNewDepartment] = useState({ departmentName: '', location: '', managerId: '' });
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      navigate('/');
    }
  }, [navigate, token]);

  const handleAddDepartment = async (e) => {
    e.preventDefault();
    console.log('Sending request with token:', token); // Debug token
    try {
      const command = {
        departmentName: newDepartment.departmentName,
        location: newDepartment.location,
        managerId: newDepartment.managerId ? parseInt(newDepartment.managerId) : null
      };
      console.log('Request body:', command); // Debug body
      const response = await commandApi.post('api/Departments', command, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Response:', response); // Debug response
      if (response.data && response.data.isSuccess) {
        alert('Phòng ban đã được thêm thành công!');
        navigate('/departments');
      } else {
        throw new Error(response.data.error?.message || 'Thêm phòng ban thất bại.');
      }
    } catch (err) {
      console.error('API Add Department Error:', err.response ? err.response.data : err.message);
      if (err.message.includes('Network Error')) {
        setError('Không thể kết nối đến server. Vui lòng kiểm tra server backend.');
      } else {
        setError(err.message || 'Không thể thêm phòng ban. Vui lòng thử lại.');
      }
    }
  };

  return (
    <div className="container mt-5">
      <h2>Thêm phòng ban mới</h2>
      {error && <div className="alert alert-danger">{error}</div>}
      <form onSubmit={handleAddDepartment}>
        <div className="mb-3">
          <label className="form-label">Tên phòng ban</label>
          <input
            type="text"
            className="form-control"
            value={newDepartment.departmentName}
            onChange={(e) => setNewDepartment({ ...newDepartment, departmentName: e.target.value })}
            placeholder="Nhập tên phòng ban"
            required
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Địa điểm</label>
          <input
            type="text"
            className="form-control"
            value={newDepartment.location}
            onChange={(e) => setNewDepartment({ ...newDepartment, location: e.target.value })}
            placeholder="Nhập địa điểm"
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Mã quản lý (nếu có)</label>
          <input
            type="number"
            className="form-control"
            value={newDepartment.managerId}
            onChange={(e) => setNewDepartment({ ...newDepartment, managerId: e.target.value })}
            placeholder="Nhập mã quản lý"
          />
        </div>
        <button type="submit" className="btn btn-primary me-2">Lưu</button>
        <button type="button" className="btn btn-secondary" onClick={() => navigate('/departments')}>Hủy</button>
      </form>
    </div>
  );
}

export default AddDepartments;