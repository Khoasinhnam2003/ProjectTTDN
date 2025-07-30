import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { queryApi, commandApi } from '../../../api';

function SkillsManagerDashboard() {
  const [userRoleSkills, setUserRoleSkills] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [user, setUser] = useState(null);
  const navigate = useNavigate();
  const location = useLocation();
  const token = localStorage.getItem('token');

  const getActivePage = () => {
    switch (location.pathname) {
      case '/': return 'home';
      case '/manager-dashboard': return 'employees';
      case '/attendances-manager': return 'attendances';
      case '/contracts-manager': return 'contracts';
      case '/salary-histories-manager': return 'salaryHistories';
      case '/skills-manager': return 'skills';
      case '/account-management-manager': return 'accountManagement';
      default: return 'manager';
    }
  };

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
          setError('You do not have permission to view this page.');
          navigate('/');
        }
      } catch (err) {
        console.error('Error parsing user data:', err);
        setError('Invalid user data in storage. Please log in again.');
        localStorage.removeItem('user');
        navigate('/');
      }
    } else {
      setError('No user data found. Please log in.');
      navigate('/');
    }
  }, [navigate, token]);

  useEffect(() => {
    const fetchSkillsForUserRole = async () => {
      if (!user || !user.roles.includes('Manager')) return;
      try {
        setLoading(true);

        const employeesResponse = await queryApi.get('api/Employees/by-role/User', {
          headers: { Authorization: `Bearer ${token}` },
          params: { PageNumber: 1, PageSize: 100 },
        });

        const employeesData = employeesResponse.data.values?.['$values'] || [];
        if (!Array.isArray(employeesData)) {
          throw new Error('Employee data is not an array.');
        }

        const skillsData = await Promise.all(
          employeesData.map(async (emp) => {
            try {
              const skillsResponse = await queryApi.get(`api/Skill/by-employee/${emp.employeeId}`, {
                headers: { Authorization: `Bearer ${token}` },
                params: { PageNumber: 1, PageSize: 100 },
              });

              const skills = skillsResponse.data.$values || skillsResponse.data;
              if (!Array.isArray(skills)) {
                throw new Error('Skills data is not an array.');
              }

              return skills.map(skill => ({
                skillId: skill.skillId,
                employeeId: emp.employeeId,
                employeeName: emp.fullName || 'Not available',
                skillName: skill.skillName || 'Not available',
                proficiencyLevel: skill.proficiencyLevel || 'Not available',
                description: skill.description || 'No description',
              }));
            } catch (err) {
              console.error(`Error fetching skills for employee ${emp.employeeId}:`, err);
              return [];
            }
          })
        );

        const flattenedSkills = skillsData.flat();
        setUserRoleSkills(flattenedSkills);
        setLoading(false);
      } catch (err) {
        console.error('API Error:', err.response ? err.response.data : err.message);
        if (err.response?.status === 403) {
          setError('You do not have permission to view this information.');
        } else if (err.response?.status === 404) {
          setUserRoleSkills([]);
          setError('No skills found for employees with User role.');
        } else {
          setError(err.response?.data?.Message || 'Unable to load skills information.');
        }
        setLoading(false);
        if (err.response?.status === 401 || err.response?.status === 403) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      }
    };

    if (token && user?.roles.includes('Manager')) {
      fetchSkillsForUserRole();
    }
  }, [token, user, navigate]);

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
        throw new Error('Invalid server response.');
      }

      if (!logoutResponse.data.isSuccess) {
        throw new Error(logoutResponse.data.error?.message || 'Logout failed.');
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
      setError(err.response?.data?.error?.message || err.message || 'Logout failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteSkill = async (skillId) => {
    if (!window.confirm('Bạn có chắc muốn xóa kỹ năng này?')) return;
    setLoading(true);
    try {
      const response = await commandApi.delete(`api/Skills/${skillId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.data || typeof response.data !== 'object') {
        throw new Error('Invalid server response.');
      }

      if (!response.data.isSuccess) {
        throw new Error(response.data.error?.message || 'Delete skill failed.');
      }

      setUserRoleSkills(userRoleSkills.filter(skill => skill.skillId !== skillId));
      alert('Xóa kỹ năng thành công!');
    } catch (err) {
      console.error('Delete skill error:', err.response ? err.response.data : err.message);
      setError(err.response?.data?.error?.message || err.message || 'Xóa kỹ năng thất bại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Danh sách kỹ năng vai trò User</h2>
        <div>
          <button
            className="btn btn-success me-2"
            onClick={() => navigate('/add-skills-role-manager')}
            disabled={loading}
          >
            Thêm kỹ năng
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

      <div className="mb-4 d-flex flex-wrap gap-2" style={{ flexShrink: 0, position: 'relative' }}>
        <button
          className={`btn ${getActivePage() === 'employees' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/manager-dashboard')}
          style={{ minWidth: '150px', minHeight: '38px', order: 1 }}
        >
          Danh sách nhân viên
        </button>
        <button
          className={`btn ${getActivePage() === 'attendances' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/attendances-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 2 }}
        >
          Danh sách chấm công
        </button>
        <button
          className={`btn ${getActivePage() === 'contracts' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/contracts-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 3 }}
        >
          Danh sách hợp đồng
        </button>
        <button
          className={`btn ${getActivePage() === 'salaryHistories' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/salary-histories-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 4 }}
        >
          Lịch sử lương
        </button>
        <button
          className={`btn ${getActivePage() === 'skills' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/skills-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 5 }}
        >
          Danh sách kỹ năng
        </button>
        <button
          className={`btn ${getActivePage() === 'accountManagement' ? 'btn-primary' : 'btn-info'}`}
          onClick={() => navigate('/account-management-manager')}
          style={{ minWidth: '150px', minHeight: '38px', order: 6 }}
        >
          Quản lý tài khoản
        </button>
      </div>

      {loading && <div className="text-center">Đang tải...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div className="table-responsive">
          <table className="table table-striped table-bordered">
            <thead className="thead-dark">
              <tr>
                <th>Skill ID</th>
                <th>Employee ID</th>
                <th>Employee Name</th>
                <th>Skill Name</th>
                <th>Proficiency Level</th>
                <th>Description</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {userRoleSkills.length > 0 ? (
                userRoleSkills.map((skill) => (
                  <tr key={skill.skillId}>
                    <td>{skill.skillId}</td>
                    <td>{skill.employeeId}</td>
                    <td>{skill.employeeName}</td>
                    <td>{skill.skillName}</td>
                    <td>{skill.proficiencyLevel}</td>
                    <td>{skill.description}</td>
                    <td>
                      <button
                        className="btn btn-warning me-2"
                        onClick={() => navigate(`/update-skill-role-manager/${skill.skillId}`)}
                        disabled={loading}
                      >
                        Chỉnh sửa
                      </button>
                      <button
                        className="btn btn-danger"
                        onClick={() => handleDeleteSkill(skill.skillId)}
                        disabled={loading}
                      >
                        Xóa
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="7" className="text-center">
                    Không có kỹ năng nào cho nhân viên với vai trò User.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default SkillsManagerDashboard;