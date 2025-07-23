import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { queryApi, commandApi } from '../../api';

function EditEmployee() {
  const { employeeId } = useParams();
  const [employee, setEmployee] = useState({
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
  const [departments, setDepartments] = useState([]);
  const [positions, setPositions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (!token) {
      navigate('/');
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        // Fetch employee data
        const employeeResponse = await queryApi.get(`api/Employees/${employeeId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const employeeData = employeeResponse.data;
        console.log('API Employee Response:', employeeData);
        if (employeeData && employeeData.employeeId) {
          setEmployee({
            firstName: employeeData.firstName || '',
            lastName: employeeData.lastName || '',
            email: employeeData.email || '',
            phone: employeeData.phone || '',
            dateOfBirth: employeeData.dateOfBirth ? new Date(employeeData.dateOfBirth).toISOString().split('T')[0] : '',
            hireDate: employeeData.hireDate ? new Date(employeeData.hireDate).toISOString().split('T')[0] : '',
            departmentId: employeeData.departmentId || '',
            positionId: employeeData.positionId || '',
            isActive: employeeData.isActive ?? true,
          });
        }

        // Fetch departments and positions for dropdowns
        const [deptResponse, posResponse] = await Promise.all([
          queryApi.get('api/Departments', { headers: { Authorization: `Bearer ${token}` } }),
          queryApi.get('api/Positions', { headers: { Authorization: `Bearer ${token}` } }),
        ]);
        setDepartments(deptResponse.data.$values || []);
        setPositions(posResponse.data.$values || []);
        setLoading(false);
      } catch (err) {
        console.error('API Error:', err.response ? err.response.data : err.message);
        setError(err.response?.data?.Message || 'Unable to load employee information.');
        setLoading(false);
        if (err.response?.status === 401 || err.response?.status === 403) {
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      }
    };

    fetchData();
  }, [employeeId, token, navigate]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setEmployee((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const payload = {
        employeeId: parseInt(employeeId),
        firstName: employee.firstName,
        lastName: employee.lastName,
        email: employee.email,
        phone: employee.phone || null,
        dateOfBirth: employee.dateOfBirth || null,
        hireDate: employee.hireDate || new Date().toISOString().split('T')[0],
        departmentId: employee.departmentId ? parseInt(employee.departmentId) : null,
        positionId: employee.positionId ? parseInt(employee.positionId) : null,
        isActive: employee.isActive,
      };
      console.log('Sending Payload:', payload); // Debug payload
      const response = await commandApi.put(`api/Employees/${employeeId}`, payload, {
        headers: { Authorization: `Bearer ${token}` },
      });
      console.log('Update Response:', response.data);
      if (response.data && response.data.isSuccess) {
        alert('Cập nhật thông tin nhân viên thành công!');
        const fromAdmin = window.history.state?.from === '/admin-dashboard';
        navigate(fromAdmin ? '/admin-dashboard' : '/user-dashboard');
      } else {
        throw new Error(response.data.error?.message || 'Update failed.');
      }
    } catch (err) {
      console.error('Update Error:', err.response ? err.response.data : err.message);
      setError(err.response?.data?.error?.message || err.message || 'Failed to update employee information.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Edit Employee Information</h2>
        <button
          className="btn btn-secondary"
          onClick={() => navigate(-1)}
        >
          Back
        </button>
      </div>

      {loading && <div className="text-center">Loading...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <form onSubmit={handleSubmit} className="card p-4 shadow-sm">
          <div className="mb-3">
            <label htmlFor="firstName" className="form-label">First Name</label>
            <input
              type="text"
              id="firstName"
              name="firstName"
              value={employee.firstName}
              onChange={handleChange}
              className="form-control"
              placeholder="Enter first name"
              required
            />
          </div>
          <div className="mb-3">
            <label htmlFor="lastName" className="form-label">Last Name</label>
            <input
              type="text"
              id="lastName"
              name="lastName"
              value={employee.lastName}
              onChange={handleChange}
              className="form-control"
              placeholder="Enter last name"
              required
            />
          </div>
          <div className="mb-3">
            <label htmlFor="email" className="form-label">Email</label>
            <input
              type="email"
              id="email"
              name="email"
              value={employee.email}
              onChange={handleChange}
              className="form-control"
              placeholder="Enter email"
              required
            />
          </div>
          <div className="mb-3">
            <label htmlFor="phone" className="form-label">Phone</label>
            <input
              type="text"
              id="phone"
              name="phone"
              value={employee.phone}
              onChange={handleChange}
              className="form-control"
              placeholder="Enter phone number"
            />
          </div>
          <div className="mb-3">
            <label htmlFor="dateOfBirth" className="form-label">Date of Birth</label>
            <input
              type="date"
              id="dateOfBirth"
              name="dateOfBirth"
              value={employee.dateOfBirth}
              onChange={handleChange}
              className="form-control"
            />
          </div>
          <div className="mb-3">
            <label htmlFor="hireDate" className="form-label">Hire Date</label>
            <input
              type="date"
              id="hireDate"
              name="hireDate"
              value={employee.hireDate}
              onChange={handleChange}
              className="form-control"
              required
            />
          </div>
          <div className="mb-3">
            <label htmlFor="departmentId" className="form-label">Department</label>
            <select
              className="form-control"
              id="departmentId"
              name="departmentId"
              value={employee.departmentId}
              onChange={handleChange}
              required
            >
              <option value="">Select Department</option>
              {departments.map((dept) => (
                <option key={dept.departmentId} value={dept.departmentId}>
                  {dept.departmentName}
                </option>
              ))}
            </select>
          </div>
          <div className="mb-3">
            <label htmlFor="positionId" className="form-label">Position</label>
            <select
              className="form-control"
              id="positionId"
              name="positionId"
              value={employee.positionId}
              onChange={handleChange}
              required
            >
              <option value="">Select Position</option>
              {positions.map((pos) => (
                <option key={pos.positionId} value={pos.positionId}>
                  {pos.positionName}
                </option>
              ))}
            </select>
          </div>
          <div className="mb-3 form-check">
            <input
              type="checkbox"
              className="form-check-input"
              id="isActive"
              name="isActive"
              checked={employee.isActive}
              onChange={handleChange}
            />
            <label className="form-check-label" htmlFor="isActive">Active</label>
          </div>
          <button type="submit" className="btn btn-primary w-100" disabled={loading}>
            {loading ? 'Saving...' : 'Save Changes'}
          </button>
        </form>
      )}
    </div>
  );
}

export default EditEmployee;