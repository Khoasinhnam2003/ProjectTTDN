import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateDepartments() {
  const { departmentId } = useParams();
  const [department, setDepartment] = useState({ departmentName: '', location: '', managerId: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    const fetchDepartment = async () => {
      if (!token) {
        navigate('/');
        return;
      }
      if (!departmentId || isNaN(parseInt(departmentId))) {
        setError('ID phòng ban không hợp lệ.');
        setLoading(false);
        return;
      }
      try {
        setLoading(true);
        console.log('Sending GET request to:', `api/Departments/${departmentId}`);
        console.log('Authorization token:', token);
        const response = await queryApi.get(`api/Departments/${departmentId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        console.log('Raw API Response:', response);
        console.log('Response Data:', response.data);

        const deptData = response.data;
        if (!deptData) {
          throw new Error('Không nhận được dữ liệu từ API.');
        }
        // Hỗ trợ cả DepartmentId (chữ D hoa) và departmentId (chữ d thường)
        const deptId = deptData.DepartmentId || deptData.departmentId;
        if (!deptId) {
          throw new Error('Phòng ban không tồn tại hoặc dữ liệu không đúng định dạng.');
        }

        setDepartment({
          departmentName: deptData.DepartmentName || deptData.departmentName || '',
          location: deptData.Location || deptData.location || '',
          managerId: '' // ManagerId không có trong response, để trống
        });
        setLoading(false);
      } catch (err) {
        console.error('API Fetch Department Error:', err.message);
        console.error('Error Response:', err.response ? err.response.data : 'No response data');
        console.error('Error Status:', err.response ? err.response.status : 'No status');
        let errorMessage = 'Không thể tải thông tin phòng ban. Vui lòng thử lại.';
        if (err.response) {
          if (err.response.status === 404) {
            errorMessage = 'Phòng ban không tồn tại.';
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
    fetchDepartment();
  }, [departmentId, navigate, token]);

  const handleUpdateDepartment = async (e) => {
    e.preventDefault();
    if (!departmentId || isNaN(parseInt(departmentId))) {
      setError('ID phòng ban không hợp lệ.');
      return;
    }
    if (!department.departmentName.trim()) {
      setError('Tên phòng ban không được để trống.');
      return;
    }
    try {
      const command = {
        departmentId: parseInt(departmentId),
        departmentName: department.departmentName,
        location: department.location,
        managerId: department.managerId ? parseInt(department.managerId) : null
      };
      console.log('Update command:', command);
      const response = await commandApi.put(`api/Departments/${departmentId}`, command, {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log('Update Response:', response);
      if (response.data && response.data.isSuccess) {
        alert('Phòng ban đã được cập nhật thành công!');
        navigate('/departments');
      } else {
        throw new Error(response.data.error?.message || 'Cập nhật phòng ban thất bại.');
      }
    } catch (err) {
      console.error('API Update Department Error:', err.response ? err.response.data : err.message);
      const errorMessage = err.response?.status === 404
        ? 'Phòng ban không tồn tại.'
        : err.response?.data?.error?.message || 'Không thể cập nhật phòng ban. Vui lòng thử lại.';
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
      <h2>Chỉnh sửa phòng ban</h2>
      <form onSubmit={handleUpdateDepartment}>
        <div className="mb-3">
          <label className="form-label">Tên phòng ban</label>
          <input
            type="text"
            className="form-control"
            value={department.departmentName}
            onChange={(e) => setDepartment({ ...department, departmentName: e.target.value })}
            placeholder="Nhập tên phòng ban"
            required
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Địa điểm</label>
          <input
            type="text"
            className="form-control"
            value={department.location}
            onChange={(e) => setDepartment({ ...department, location: e.target.value })}
            placeholder="Nhập địa điểm"
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Mã quản lý (nếu có)</label>
          <input
            type="number"
            className="form-control"
            value={department.managerId}
            onChange={(e) => setDepartment({ ...department, managerId: e.target.value })}
            placeholder="Nhập mã quản lý"
          />
        </div>
        <button type="submit" className="btn btn-primary me-2">Lưu</button>
        <button type="button" className="btn btn-secondary" onClick={() => navigate('/departments')}>Hủy</button>
      </form>
    </div>
  );
}

export default UpdateDepartments;