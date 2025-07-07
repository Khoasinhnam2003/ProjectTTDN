import { Routes, Route } from 'react-router-dom';
import Login from './Pages/Login.js';
import Dashboard from './Pages/Dashboard.js';

function App() {
  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route path="/dashboard" element={<Dashboard />} />
    </Routes>
  );
}

export default App;