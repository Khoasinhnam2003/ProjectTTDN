import axios from 'axios';

// API instance for Query backend
const queryApi = axios.create({
  baseURL: 'http://localhost:5221',
  withCredentials: true,
});

// API instance for Command backend
const commandApi = axios.create({
  baseURL: 'http://localhost:5001',
  withCredentials: true,
});

// Add request interceptor for both instances
const applyInterceptors = (instance) => {
  instance.interceptors.request.use(
    (config) => {
      const token = localStorage.getItem('token');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => {
      return Promise.reject(error);
    }
  );
};

// Apply interceptors to both instances
applyInterceptors(queryApi);
applyInterceptors(commandApi);

export { queryApi, commandApi };