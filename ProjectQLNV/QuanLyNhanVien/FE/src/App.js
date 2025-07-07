import { Routes, Route, useLocation, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import Login from './Pages/Login.js';
import UserDashboard from './Pages/UserDashboard.js';
import AdminDashboard from './Pages/AdminDashboard.js';
import AddEmployee from './Pages/AddEmployee.js';
import UpdateEmployee from './Pages/UpdateEmployee.js';

function App() {
  const navigate = useNavigate();
  const location = useLocation();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (token && location.pathname === '/') {
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      const roles = user.roles || [];
      if (roles.includes('Admin')) {
        navigate('/admin-dashboard');
      } else if (roles.includes('User')) {
        navigate('/user-dashboard');
      } else {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        navigate('/');
      }
    }
  }, [token, location, navigate]);

  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/user-dashboard" element={<UserDashboard />} />
      <Route path="/admin-dashboard" element={<AdminDashboard />} />
      <Route path="/add-employee" element={<AddEmployee />} />
      <Route path="/update-employee/:employeeId" element={<UpdateEmployee />} />
    </Routes>
  );
}

export default App;