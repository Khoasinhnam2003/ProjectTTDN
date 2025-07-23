import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { commandApi } from '../../../api';

function AddPosition() {
  const [newPosition, setNewPosition] = useState({ positionName: '', description: '', baseSalary: '' });
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      navigate('/');
    }
  }, [navigate, token]);

  const handleAddPosition = async (e) => {
    e.preventDefault();
    console.log('Sending request with token:', token);
    try {
      const command = {
        positionName: newPosition.positionName,
        description: newPosition.description,
        baseSalary: newPosition.baseSalary ? parseFloat(newPosition.baseSalary) : null
      };
      console.log('Request body:', command);
      const response = await commandApi.post('api/Positions', command, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Response:', response);
      if (response.data && response.data.isSuccess) {
        alert('Vị trí đã được thêm thành công!');
        navigate('/positions');
      } else {
        throw new Error(response.data.error?.message || 'Thêm vị trí thất bại.');
      }
    } catch (err) {
      console.error('API Add Position Error:', err.response ? err.response.data : err.message);
      if (err.message.includes('Network Error')) {
        setError('Không thể kết nối đến server. Vui lòng kiểm tra server backend.');
      } else {
        setError(err.message || 'Không thể thêm vị trí. Vui lòng thử lại.');
      }
    }
  };

  return (
    <div className="container mt-5">
      <h2>Thêm vị trí mới</h2>
      {error && <div className="alert alert-danger">{error}</div>}
      <form onSubmit={handleAddPosition}>
        <div className="mb-3">
          <label className="form-label">Tên vị trí</label>
          <input
            type="text"
            className="form-control"
            value={newPosition.positionName}
            onChange={(e) => setNewPosition({ ...newPosition, positionName: e.target.value })}
            placeholder="Nhập tên vị trí"
            required
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Mô tả</label>
          <input
            type="text"
            className="form-control"
            value={newPosition.description}
            onChange={(e) => setNewPosition({ ...newPosition, description: e.target.value })}
            placeholder="Nhập mô tả"
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Lương cơ bản</label>
          <input
            type="number"
            className="form-control"
            value={newPosition.baseSalary}
            onChange={(e) => setNewPosition({ ...newPosition, baseSalary: e.target.value })}
            placeholder="Nhập lương cơ bản"
            step="0.01"
          />
        </div>
        <button type="submit" className="btn btn-primary me-2">Lưu</button>
        <button type="button" className="btn btn-secondary" onClick={() => navigate('/positions')}>Hủy</button>
      </form>
    </div>
  );
}

export default AddPosition;