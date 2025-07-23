import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdatePosition() {
  const { positionId } = useParams();
  const [position, setPosition] = useState({ positionName: '', description: '', baseSalary: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    const fetchPosition = async () => {
      if (!token) {
        navigate('/');
        return;
      }
      if (!positionId || isNaN(parseInt(positionId))) {
        setError('ID vị trí không hợp lệ.');
        setLoading(false);
        return;
      }
      try {
        setLoading(true);
        console.log('Sending GET request to:', `api/Positions/${positionId}`);
        console.log('Authorization token:', token);
        const response = await queryApi.get(`api/Positions/${positionId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Raw API Response:', response);
        console.log('Response Data:', response.data);

        const posData = response.data;
        if (!posData) {
          throw new Error('Không nhận được dữ liệu từ API.');
        }
        const posId = posData.positionId || posData.PositionId;
        if (!posId) {
          throw new Error('Vị trí không tồn tại hoặc dữ liệu không đúng định dạng.');
        }

        setPosition({
          positionName: posData.positionName || posData.PositionName || '',
          description: posData.description || posData.Description || '',
          baseSalary: posData.baseSalary || posData.BaseSalary || ''
        });
        setLoading(false);
      } catch (err) {
        console.error('API Fetch Position Error:', err.message);
        console.error('Error Response:', err.response ? err.response.data : 'No response data');
        console.error('Error Status:', err.response ? err.response.status : 'No status');
        let errorMessage = 'Không thể tải thông tin vị trí. Vui lòng thử lại.';
        if (err.response) {
          if (err.response.status === 404) {
            errorMessage = 'Vị trí không tồn tại.';
          } else if (err.response.status === 401 || err.response.status === 403) {
            errorMessage = 'Không có quyền truy cập hoặc phiên đăng nhập hết hạn.';
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            navigate('/');
          } else {
            errorMessage = err.response.data?.message || err.message;
          }
        } else {
          errorMessage = err.message;
        }
        setError(errorMessage);
        setLoading(false);
      }
    };
    fetchPosition();
  }, [positionId, navigate, token]);

  const handleUpdatePosition = async (e) => {
    e.preventDefault();
    if (!positionId || isNaN(parseInt(positionId))) {
      setError('ID vị trí không hợp lệ.');
      return;
    }
    if (!position.positionName.trim()) {
      setError('Tên vị trí không được để trống.');
      return;
    }
    try {
      const command = {
        positionId: parseInt(positionId),
        positionName: position.positionName,
        description: position.description,
        baseSalary: position.baseSalary ? parseFloat(position.baseSalary) : null
      };
      console.log('Update command:', command);
      const response = await commandApi.put(`api/Positions/${positionId}`, command, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Update Response:', response);
      if (response.data && response.data.isSuccess) {
        alert('Vị trí đã được cập nhật thành công!');
        navigate('/positions');
      } else {
        throw new Error(response.data.error?.message || 'Cập nhật vị trí thất bại.');
      }
    } catch (err) {
      console.error('API Update Position Error:', err.response ? err.response.data : err.message);
      const errorMessage = err.response?.status === 404
        ? 'Vị trí không tồn tại.'
        : err.response?.data?.error?.message || 'Không thể cập nhật vị trí. Vui lòng thử lại.';
      setError(errorMessage);
      if (err.response?.status === 401 || err.response?.status === 403) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      }
    }
  };

  if (loading) return <div className="text-center">Đang tải...</div>;
  if (error) return <div className="alert alert-danger">{error}</div>;

  return (
    <div className="container mt-5">
      <h2>Chỉnh sửa vị trí</h2>
      <form onSubmit={handleUpdatePosition}>
        <div className="mb-3">
          <label className="form-label">Tên vị trí</label>
          <input
            type="text"
            className="form-control"
            value={position.positionName}
            onChange={(e) => setPosition({ ...position, positionName: e.target.value })}
            placeholder="Nhập tên vị trí"
            required
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Mô tả</label>
          <input
            type="text"
            className="form-control"
            value={position.description}
            onChange={(e) => setPosition({ ...position, description: e.target.value })}
            placeholder="Nhập mô tả"
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Lương cơ bản</label>
          <input
            type="number"
            className="form-control"
            value={position.baseSalary}
            onChange={(e) => setPosition({ ...position, baseSalary: e.target.value })}
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

export default UpdatePosition;