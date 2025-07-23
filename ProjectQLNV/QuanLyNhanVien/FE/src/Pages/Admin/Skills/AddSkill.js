import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { commandApi } from '../../../api';

function AddSkill() {
  const [formData, setFormData] = useState({
    employeeId: '',
    skillName: '',
    proficiencyLevel: '',
    description: ''
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  const validateForm = () => {
    if (!formData.employeeId || isNaN(parseInt(formData.employeeId))) {
      return 'Mã nhân viên phải là một số hợp lệ.';
    }
    if (!formData.skillName) {
      return 'Tên kỹ năng không được để trống.';
    }
    if (!/^[a-zA-Z\s]+$/.test(formData.skillName)) {
      return 'Tên kỹ năng chỉ được chứa chữ cái và khoảng trắng.';
    }
    if (formData.skillName.length > 100) {
      return 'Tên kỹ năng tối đa 100 ký tự.';
    }
    if (formData.proficiencyLevel && formData.proficiencyLevel.length > 50) {
      return 'Mức độ thành thạo tối đa 50 ký tự.';
    }
    if (formData.description && formData.description.length > 200) {
      return 'Mô tả tối đa 200 ký tự.';
    }
    return null;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      setLoading(false);
      return;
    }

    try {
      const payload = {
        employeeId: parseInt(formData.employeeId, 10),
        skillName: formData.skillName,
        proficiencyLevel: formData.proficiencyLevel || null,
        description: formData.description || null
      };
      console.log('Payload:', JSON.stringify(payload, null, 2));

      const response = await commandApi.post('api/Skills', payload, {
        headers: { Authorization: `Bearer ${token}` }
      });

      if (response.data && response.data.isSuccess) {
        alert('Kỹ năng đã được tạo thành công!');
        navigate('/skills');
      } else {
        throw new Error(response.data.error?.message || 'Tạo kỹ năng thất bại.');
      }
    } catch (err) {
      console.error('Lỗi chi tiết:', err.response ? err.response.data : err);
      if (err.response?.status === 400 && err.response.data.errors) {
        const errorMessages = Object.values(err.response.data.errors).flat().join('; ');
        setError(errorMessages || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.');
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        setError('Phiên đăng nhập hết hạn hoặc không có quyền truy cập.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else {
        setError(err.response?.data?.error?.message || err.message || 'Đã xảy ra lỗi khi tạo kỹ năng.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex align-items-center justify-content-center min-vh-100 bg-light">
      <div className="bg-white p-4 rounded shadow w-100" style={{ maxWidth: '500px' }}>
        <h2 className="text-center mb-4">Thêm kỹ năng</h2>
        {error && <div className="alert alert-danger">{error}</div>}
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
            <label htmlFor="skillName" className="form-label">Tên kỹ năng</label>
            <input
              type="text"
              id="skillName"
              name="skillName"
              value={formData.skillName}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập tên kỹ năng"
              required
              disabled={loading}
              maxLength={100}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="proficiencyLevel" className="form-label">Mức độ thành thạo</label>
            <input
              type="text"
              id="proficiencyLevel"
              name="proficiencyLevel"
              value={formData.proficiencyLevel}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập mức độ thành thạo (ví dụ: Sơ cấp, Trung bình, Cao cấp)"
              disabled={loading}
              maxLength={50}
            />
          </div>
          <div className="mb-3">
            <label htmlFor="description" className="form-label">Mô tả</label>
            <textarea
              id="description"
              name="description"
              value={formData.description}
              onChange={handleChange}
              className="form-control"
              placeholder="Nhập mô tả kỹ năng"
              disabled={loading}
              maxLength={200}
            />
          </div>
          <button type="submit" className="btn btn-primary w-100" disabled={loading}>
            {loading ? 'Đang tạo...' : 'Tạo kỹ năng'}
          </button>
          <button
            type="button"
            className="btn btn-secondary w-100 mt-2"
            onClick={() => navigate('/skills')}
            disabled={loading}
          >
            Quay lại
          </button>
        </form>
      </div>
    </div>
  );
}

export default AddSkill;