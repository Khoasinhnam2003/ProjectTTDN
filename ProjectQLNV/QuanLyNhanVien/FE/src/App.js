import { Routes, Route } from 'react-router-dom';
import Login from './Pages/Login.js';
import Dashboard from './Pages/Dashboard.js';
import AddEmployee from './Pages/AddEmployee.js'; 
import UpdateEmployee from './Pages/UpdateEmployee.js';

function App() {
  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/dashboard" element={<Dashboard />} />
      <Route path="/add-employee" element={<AddEmployee />} />
      <Route path="/update-employee/:employeeId" element={<UpdateEmployee />} />
    </Routes>
  );
}

export default App;