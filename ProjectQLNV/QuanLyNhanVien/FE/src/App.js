import { Routes, Route } from 'react-router-dom';
import Login from './Pages/Login.js';
import Dashboard from './Pages/Dashboard.js';
import AddEmployee from './Pages/AddEmployee.js'; 

function App() {
  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/dashboard" element={<Dashboard />} />
      <Route path="/add-employee" element={<AddEmployee />} />
    </Routes>
  );
}

export default App;