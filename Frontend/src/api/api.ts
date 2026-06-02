import axios from "axios";

const api = axios.create({
  baseURL:import.meta.env.VITE_API_BASE_URL,
  withCredentials: true, // important for cookies
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

   
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        
        await axios.post(
          "https://localhost:7023/api/User/refresh-token",
          {},
          { withCredentials: true }
        );

        
        return api(originalRequest);
      } catch (err) {
        console.log("Refresh token failed, redirecting to Register");
        // window.location.href = "/Register";
        return Promise.reject(err);
      }
    }

    return Promise.reject(error);
  }
);

export default api;