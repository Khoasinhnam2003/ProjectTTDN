import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { commandApi } from '../../../api';

function AddSkillsRoleManager() {
  const [skill, setSkill] = useState({
    employeeId: '',
    skillName: '',
    proficiencyLevel: '',
    description: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [user, setUser] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      navigate('/');
      return;
    }

    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser);
        const roleArray = parsedUser.roles && parsedUser.roles.$values ? parsedUser.roles.$values : parsedUser.roles || [];
        setUser({ ...parsedUser, roles: roleArray });
        if (!roleArray.includes('Manager')) {
          setError('Bạn không có quyền truy cập trang này.');
          navigate('/');
        }
      } catch (err) {
        console.error('Lỗi phân tích dữ liệu người dùng:', err);
        setError('Dữ liệu người dùng không hợp lệ. Vui lòng đăng nhập lại.');
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      setError('Không tìm thấy dữ liệu người dùng. Vui lòng đăng nhập.');
      navigate('/');
    }
  }, [navigate, token]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setSkill(prev => ({ ...prev, [name]: value }));
    setError('');
  };

  const validateForm = () => {
    if (!skill.employeeId || isNaN(parseInt(skill.employeeId))) {
      return 'Mã nhân viên phải là một số hợp lệ.';
    }
    if (!skill.skillName) {
      return 'Tên kỹ năng không được để trống.';
    }
    if (!/^[\p{L}\s]+$/u.test(skill.skillName)) {
      return 'Tên kỹ năng chỉ được chứa chữ cái và khoảng trắng (không chứa số hoặc ký tự đặc biệt).';
    }
    if (skill.skillName.length > 100) {
      return 'Tên kỹ năng tối đa 100 ký tự.';
    }
    if (skill.proficiencyLevel && skill.proficiencyLevel.length > 50) {
      return 'Mức độ thành thạo tối đa 50 ký tự.';
    }
    if (skill.description && skill.description.length > 200) {
      return 'Mô tả tối đa 200 ký tự.';
    }
    return null;
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
        employeeId: parseInt(skill.employeeId, 10),
        skillName: skill.skillName.trim(),
        proficiencyLevel: skill.proficiencyLevel || null,
        description: skill.description ? skill.description.trim() : null
      };
      console.log('Sending add skill request:', {
        url: 'http://localhost:5001/api/Skills',
        payload,
        headers: { Authorization: `Bearer ${token.slice(0, 20)}...` }
      });

      const response = await commandApi.post('api/Skills', payload, {
        headers: { Authorization: `Bearer ${token}` },
      });

      console.log('Add skill response:', {
        status: response.status,
        data: response.data,
        headers: response.headers
      });

      if (!response.data || typeof response.data !== 'object') {
        throw new Error('Phản hồi từ server không hợp lệ.');
      }

      if (!response.data.isSuccess) {
        throw new Error(response.data.error?.message || 'Thêm kỹ năng thất bại.');
      }

      alert('Thêm kỹ năng thành công!');
      navigate('/skills-manager');
    } catch (err) {
      console.error('Add skill error:', {
        message: err.message,
        response: err.response ? {
          status: err.response.status,
          data: err.response.data,
          headers: err.response.headers
        } : null
      });
      setError(err.response?.data?.error?.message || err.message || 'Thêm kỹ năng thất bại.');
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = async () => {
    if (!token) {
      navigate('/');
      return;
    }
    setLoading(true);
    try {
      const logoutResponse = await commandApi.post('api/Authentication/logout', {}, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!logoutResponse.data || typeof logoutResponse.data !== 'object') {
        throw new Error('Phản hồi từ server không hợp lệ.');
      }

      if (!logoutResponse.data.isSuccess) {
        throw new Error(logoutResponse.data.error?.message || 'Đăng xuất thất bại.');
      }

      const data = logoutResponse.data.data || logoutResponse.data;
      const checkOutTime = data.CheckOutTime || data.checkOutTime;

      if (checkOutTime) {
        localStorage.setItem('checkOutTime', checkOutTime);
        alert(`Đăng xuất thành công! Thời gian check-out: ${new Date(checkOutTime).toLocaleString('vi-VN', {
          timeZone: 'Asia/Ho_Chi_Minh',
          dateStyle: 'short',
          timeStyle: 'medium',
        })}`);
      } else {
        alert('Đăng xuất thành công! Không có thời gian check-out.');
      }

      localStorage.removeItem('token');
      localStorage.removeItem('user');
      localStorage.removeItem('checkInTime');
      navigate('/');
    } catch (err) {
      console.error('Logout error:', err.response ? err.response.data : err.message);
      setError(err.response?.data?.error?.message || err.message || 'Đăng xuất thất bại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Thêm kỹ năng mới</h2>
        <div>
          <button
            className="btn btn-primary me-2"
            onClick={() => navigate('/skills-manager')}
            disabled={loading}
          >
            Quay lại
          </button>
          <button
            className="btn btn-danger"
            onClick={handleLogout}
            disabled={loading}
          >
            {loading ? 'Đang đăng xuất...' : 'Đăng xuất'}
          </button>
        </div>
      </div>

      {user && (
        <p className="mb-4">
          Xin chào, {user.username}! Bạn có vai trò: {Array.isArray(user.roles) ? user.roles.join(', ') : 'Không có vai trò'}
        </p>
      )}

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div className="card">
          <div className="card-body">
            <h5 className="card-title">Thêm thông tin kỹ năng</h5>
            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <label className="form-label">Employee ID</label>
                <input
                  type="number"
                  className="form-control"
                  name="employeeId"
                  value={skill.employeeId}
                  onChange={handleInputChange}
                  required
                  disabled={loading}
                />
              </div>
              <div className="mb-3">
                <label className="form-label">Skill Name</label>
                <input
                  type="text"
                  className="form-control"
                  name="skillName"
                  value={skill.skillName}
                  onChange={handleInputChange}
                  required
                  maxLength="100"
                  placeholder="Nhập tên kỹ năng (chỉ chứa chữ cái và khoảng trắng, tối đa 100 ký tự)"
                />
              </div>
              <div className="mb-3">
                <label className="form-label">Proficiency Level</label>
                <select
                  className="form-select"
                  name="proficiencyLevel"
                  value={skill.proficiencyLevel}
                  onChange={handleInputChange}
                  required
                >
                  <option value="">Chọn cấp độ thành thạo</option>
                  <option value="Beginner">Beginner</option>
                  <option value="Intermediate">Intermediate</option>
                  <option value="Advanced">Advanced</option>
                  <option value="Expert">Expert</option>
                </select>
              </div>
              <div className="mb-3">
                <label className="form-label">Description</label>
                <textarea
                  className="form-control"
                  name="description"
                  value={skill.description}
                  onChange={handleInputChange}
                  rows="4"
                  maxLength="200"
                  placeholder="Nhập mô tả (tối đa 200 ký tự)"
                />
              </div>
              <div className="d-flex justify-content-end">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading}
                >
                  {loading ? 'Đang lưu...' : 'Lưu kỹ năng'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default AddSkillsRoleManager;