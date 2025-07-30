import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function UpdateSkillRoleManager() {
  const [skill, setSkill] = useState({
    skillId: '',
    employeeId: '',
    employeeName: '',
    skillName: '',
    proficiencyLevel: '',
    description: ''
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [user, setUser] = useState(null);
  const navigate = useNavigate();
  const { skillId } = useParams();
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

  useEffect(() => {
    const fetchSkill = async () => {
      if (!user || !user.roles.includes('Manager')) return;
      try {
        setLoading(true);
        console.log(`Fetching skill with ID: ${skillId}`);
        const response = await queryApi.get(`api/Skill/${skillId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        console.log('Fetch skill response:', {
          status: response.status,
          data: response.data,
          headers: response.headers
        });
        if (!response.data) {
          throw new Error('Dữ liệu kỹ năng không hợp lệ.');
        }

        setSkill({
          skillId: response.data.skillId || response.data.SkillId,
          employeeId: response.data.employeeId || response.data.EmployeeId,
          employeeName: response.data.employeeName || response.data.EmployeeName || 'Không có thông tin',
          skillName: response.data.skillName || response.data.SkillName || '',
          proficiencyLevel: response.data.proficiencyLevel || response.data.ProficiencyLevel || '',
          description: response.data.description || response.data.Description || ''
        });
        setLoading(false);
      } catch (err) {
        console.error('Lỗi khi tải kỹ năng:', {
          message: err.message,
          response: err.response ? { status: err.response.status, data: err.response.data, headers: err.response.headers } : null
        });
        if (err.response?.status === 404) {
          setError('Không tìm thấy kỹ năng. Vui lòng kiểm tra ID kỹ năng.');
        } else if (err.response?.status === 403 || err.response?.status === 401) {
          setError('Bạn không có quyền xem kỹ năng này.');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        } else {
          setError(err.response?.data?.Message || 'Không thể tải thông tin kỹ năng.');
        }
        setLoading(false);
      }
    };

    if (token && user?.roles.includes('Manager')) {
      fetchSkill();
    }
  }, [token, user, skillId, navigate]);

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
        skillId: parseInt(skill.skillId, 10),
        employeeId: parseInt(skill.employeeId, 10),
        skillName: skill.skillName.trim(),
        proficiencyLevel: skill.proficiencyLevel || null,
        description: skill.description ? skill.description.trim() : null
      };
      console.log('Sending update skill request:', {
        url: `http://localhost:5001/api/Skills/${skillId}`,
        payload,
        headers: { Authorization: `Bearer ${token.slice(0, 20)}...` }
      });

      const response = await commandApi.put(`api/Skills/${skillId}`, payload, {
        headers: { Authorization: `Bearer ${token}` },
      });

      console.log('Update skill response (raw):', response.data);

      if (!response.data || typeof response.data !== 'object') {
        throw new Error('Phản hồi từ server không hợp lệ.');
      }

      if (!response.data.isSuccess) { // Đổi từ IsSuccess thành isSuccess
        const errorMessage = response.data.Error?.Message || response.data.Message || response.data.error?.message ||
          'Cập nhật kỹ năng thất bại do lỗi không xác định từ server.';
        throw new Error(errorMessage);
      }

      alert('Cập nhật kỹ năng thành công!');
      navigate('/skills-manager');
    } catch (err) {
      console.error('Lỗi khi cập nhật kỹ năng:', {
        message: err.message,
        response: err.response ? {
          status: err.response.status,
          data: err.response.data,
          headers: err.response.headers
        } : { status: null, data: null, headers: null }
      });
      if (err.response?.status === 404) {
        setError('Kỹ năng không tồn tại hoặc endpoint không đúng (PUT api/Skills/{skillId}).');
      } else if (err.response?.status === 400) {
        const errorMessage = err.response.data.Error?.Message || err.response.data.Message ||
          (err.response.data.errors ? Object.values(err.response.data.errors).flat().join('; ') : 'Dữ liệu không hợp lệ.');
        setError(errorMessage || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra: Tên kỹ năng phải duy nhất cho nhân viên, chỉ chứa chữ cái và khoảng trắng, tối đa 100 ký tự.');
      } else if (err.response?.status === 403 || err.response?.status === 401) {
        setError('Bạn không có quyền cập nhật kỹ năng này. Vui lòng kiểm tra token có vai trò Manager.');
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      } else {
        setError(err.message || 'Cập nhật kỹ năng thất bại.');
      }
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
      console.log('Sending logout request');
      const logoutResponse = await commandApi.post('api/Authentication/logout', {}, {
        headers: { Authorization: `Bearer ${token}` },
      });

      console.log('Logout response:', {
        status: logoutResponse.status,
        data: logoutResponse.data,
        headers: logoutResponse.headers
      });
      if (!logoutResponse.data || typeof logoutResponse.data !== 'object') {
        throw new Error('Phản hồi từ server không hợp lệ.');
      }

      if (!logoutResponse.data.IsSuccess) {
        throw new Error(logoutResponse.data.Error?.Message || logoutResponse.data.error?.message || 'Đăng xuất thất bại.');
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
      console.error('Lỗi đăng xuất:', {
        message: err.message,
        response: err.response ? { status: err.response.status, data: err.response.data, headers: err.response.headers } : null
      });
      setError(err.response?.data?.Error?.Message || err.response?.data?.error?.message || err.message || 'Đăng xuất thất bại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Chỉnh sửa kỹ năng</h2>
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
            <h5 className="card-title">Cập nhật thông tin kỹ năng</h5>
            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <label className="form-label">Skill ID</label>
                <input
                  type="text"
                  className="form-control"
                  value={skill.skillId}
                  disabled
                />
              </div>
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
                <label className="form-label">Employee Name</label>
                <input
                  type="text"
                  className="form-control"
                  value={skill.employeeName}
                  disabled
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
                  {loading ? 'Đang lưu...' : 'Lưu thay đổi'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default UpdateSkillRoleManager;