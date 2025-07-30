import { Routes, Route, useLocation, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import Login from './Pages/Login.js';
import UserDashboard from './Pages/User/UserDashboard.js';
import AdminDashboard from './Pages/Admin/Employees/AdminDashboard.js';
import AddEmployee from './Pages/Admin/Employees/AddEmployees.js';
import UpdateEmployee from './Pages/Admin/Employees/UpdateEmployees.js';
import Departments from './Pages/Admin/Departments/DepartmentsDashboard.js';
import Positions from './Pages/Admin/Positions/PositonsDashboard.js'; 
import AddDepartments from './Pages/Admin/Departments/AddDepartments.js';
import UpdateDepartments from './Pages/Admin/Departments/UpdateDepartments.js';
import AddPosition from './Pages/Admin/Positions/AddPositions.js';
import UpdatePosition from './Pages/Admin/Positions/UpdatePositions.js';
import AccountDashboard from './Pages/Admin/Accounts/AccountDashboard.js';
import AddAccount from './Pages/Admin/Accounts/AddAccount.js';
import UpdateAccount from './Pages/Admin/Accounts/UpdateAccount.js';
import AttendanceDashboard from './Pages/Admin/Attendances/AttendanceDashboard.js';
import SalaryHistoriesDashboard from './Pages/Admin/SalaryHistories/SalaryHistoriesDashboard.js';
import UpdateSalaryHistory from './Pages/Admin/SalaryHistories/UpdateSalaryHistory.js';
import AddSalaryHistory from './Pages/Admin/SalaryHistories/AddSalaryHistory.js';
import ContractsDashboard from './Pages/Admin/Contracts/ContractsDashboard.js';
import UpdateContract from './Pages/Admin/Contracts/UpdateContract.js';
import AddContract from './Pages/Admin/Contracts/AddContract.js';
import SkillsDashboard from './Pages/Admin/Skills/SkillsDashboard.js';
import AddSkill from './Pages/Admin/Skills/AddSkill.js';
import UpdateSkill from './Pages/Admin/Skills/UpdateSkill.js';
import Register from './Pages/Register.js';
import EditEmployee from './Pages/User/EditEmployee.js';
import ManagerDashboard from './Pages/Manager/Employees/ManagerDashboard.js';
import UpdateEmployeeRoleManager from './Pages/Manager/Employees/UpdateEmployeesRoleManager.js';
import AddEmployeesRoleManager from './Pages/Manager/Employees/AddEmployeesRoleManager.js';
import AttendanceManagerDashboard from './Pages/Manager/Attendances/AttendanceManagerDashboard.js';
import ContractsManagerDashboard from './Pages/Manager/Contracts/ContractsManagerDashboard.js';
import UpdateContractRoleManager from './Pages/Manager/Contracts/UpdateContractRoleManager.js';
import AddContractRoleManager from './Pages/Manager/Contracts/AddContractRoleManager.js';
import SalaryHistoriesManagerDashboard from './Pages/Manager/SalaryHistories/SalaryHistoriesManagerDashboard.js';
import UpdateSalaryHistoryRoleManager from './Pages/Manager/SalaryHistories/UpdateSalaryHistoryRoleManager.js';
import AddSalaryHistoryRoleManager from './Pages/Manager/SalaryHistories/AddSalaryHistoryRoleManager.js';
import SkillsManagerDashboard from './Pages/Manager/Skills/SkillsManagerDashboard.js';
import UpdateSkillRoleManager from './Pages/Manager/Skills/UpdateSkillRoleManager.js';
import AddSkillsRoleManager from './Pages/Manager/Skills/AddSkillsRoleManager.js';
import AccountManagementManagerDashboard from './Pages/Manager/Accounts/AccountManagementManagerDashboard.js';
import UpdateAccountRoleManager from './Pages/Manager/Accounts/UpdateAccountRoleManager.js';

function App() {
  const navigate = useNavigate();
  const location = useLocation();
  const token = localStorage.getItem('token');

  useEffect(() => {
    if (token && location.pathname === '/') {
      const storedUser = localStorage.getItem('user');
      if (storedUser) {
        try {
          const user = JSON.parse(storedUser);
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
        } catch (err) {
          console.error('Error parsing user data in App.js:', err);
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          navigate('/');
        }
      } else {
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
      <Route path="/departments" element={<Departments />} />
      <Route path="/positions" element={<Positions />} />
      <Route path="/add-department" element={<AddDepartments />} />
      <Route path="/update-department/:departmentId" element={<UpdateDepartments />} />
      <Route path="/add-position" element={<AddPosition />} />
      <Route path="/update-position/:positionId" element={<UpdatePosition />} />
      <Route path="/account-management" element={<AccountDashboard />} />
      <Route path="/add-account" element={<AddAccount />} />
      <Route path="/update-account/:userId" element={<UpdateAccount />} />
      <Route path="/attendances" element={<AttendanceDashboard />} />
      <Route path="/salary-histories" element={<SalaryHistoriesDashboard />} />
      <Route path="/update-salary-history/:salaryHistoryId" element={<UpdateSalaryHistory />} />
      <Route path="/add-salary-history" element={<AddSalaryHistory />} />
      <Route path="/contracts" element={<ContractsDashboard />} />
      <Route path="/update-contract/:contractId" element={<UpdateContract />} />
      <Route path="/add-contract" element={<AddContract />} />
      <Route path="/skills" element={<SkillsDashboard />} />
      <Route path="/add-skill" element={<AddSkill />} />
      <Route path="/update-skill/:skillId" element={<UpdateSkill />} />
      <Route path="/register" element={<Register />} />
      <Route path="/edit-employee/:employeeId" element={<EditEmployee />} />
      <Route path="/manager-dashboard" element={<ManagerDashboard />} />
      <Route path="/update-employee-role-manager/:employeeId" element={<UpdateEmployeeRoleManager />} />
      <Route path="/add-employees-role-manager" element={<AddEmployeesRoleManager />} />
      <Route path="/attendances-manager" element={<AttendanceManagerDashboard />} />
      <Route path="/contracts-manager" element={<ContractsManagerDashboard />} />
      <Route path="/update-contract-role-manager/:contractId" element={<UpdateContractRoleManager />} />
      <Route path="/add-contract-role-manager" element={<AddContractRoleManager />} />
      <Route path="/salary-histories-manager" element={<SalaryHistoriesManagerDashboard />} />
      <Route path="/update-salary-history-role-manager/:salaryHistoryId" element={<UpdateSalaryHistoryRoleManager />} />
      <Route path="/add-salary-history-role-manager" element={<AddSalaryHistoryRoleManager />} />
      <Route path="/skills-manager" element={<SkillsManagerDashboard />} />
      <Route path="/update-skill-role-manager/:skillId" element={<UpdateSkillRoleManager />} />
      <Route path="/add-skills-role-manager" element={<AddSkillsRoleManager />} />
      <Route path="/account-management-manager" element={<AccountManagementManagerDashboard />} />
      <Route path="/update-account-role-manager/:userId" element={<UpdateAccountRoleManager />} />
    </Routes>
  );
}

export default App;