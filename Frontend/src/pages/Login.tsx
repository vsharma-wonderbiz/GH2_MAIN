import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import { Toast } from "@radix-ui/react-toast";
import { toast } from "react-toastify";

const Login: React.FC = () => {
  const navigate = useNavigate();

  const [mode, setMode] = useState<"login" | "signup">("login");
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<{
    username?: string;
    email?: string;
    password?: string;
  }>({});

  const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL,
    withCredentials: true,
    headers: {
      "Content-Type": "application/json",
    },
  });

  const validate = () => {
    const newErrors: {
      username?: string;
      email?: string;
      password?: string;
    } = {};

    if (mode === "signup" && !/^[A-Za-z0-9]{3,}$/.test(username)) {
      newErrors.username =
        "Username must be at least 3 characters (letters or numbers only).";
    }

    if (!/^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/.test(email)) {
      newErrors.email = "Invalid email format";
    }

    if (password.length < 8) {
      newErrors.password = "Password must be at least 8 characters long";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    if (mode === "login") {
      try {
        const response = await api.post("/User/Login", { email, password });
        toast.success(response.data.message || "Login successful!");
        navigate("/Dashboard"); // ✅ directly redirect on success
      } catch (err: any) {
        const errorMessage =
          err.response?.data?.message ||
          err.response?.data?.data?.message ||
          "Something went wrong, please try again.";
        alert(errorMessage);
      }
    } else if (mode === "signup") {
      try {
        const response = await api.post("/User/Register", {
          username,
          email,
          password,
        });
        console.log("Registered successfully", response.data);
        setMode("login");
      } catch (err: any) {
        const errorMessage =
          err.response?.data?.message || "Registration failed.";
        alert(errorMessage);
      }
    }
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-background text-foreground transition-colors">
      <form
        onSubmit={handleSubmit}
        className="flex flex-col gap-4 w-[90%] max-w-md bg-card border border-border rounded-2xl shadow-md p-8"
      >
        <h1 className="text-2xl font-semibold text-center mb-2">
          {mode === "signup" ? "Create Account" : "Welcome Back"}
        </h1>
        <p className="text-sm text-muted-foreground text-center mb-4">
          {mode === "signup"
            ? "Sign up to start managing your devices"
            : "Login to access your Perfomance Insight dashboard"}
        </p>

        {mode === "signup" && (
          <div>
            <label className="block text-sm mb-1 font-medium">Username</label>
            <input
              type="text"
              placeholder="Enter username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full border border-border rounded-md p-2 bg-background focus:outline-none focus:ring-2 focus:ring-primary"
            />
            {errors.username && (
              <p className="text-red-500 text-xs mt-1">{errors.username}</p>
            )}
          </div>
        )}

        <div>
          <label className="block text-sm mb-1 font-medium">Email</label>
          <input
            type="email"
            placeholder="Enter email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full border border-border rounded-md p-2 bg-background focus:outline-none focus:ring-2 focus:ring-primary"
          />
          {errors.email && (
            <p className="text-red-500 text-xs mt-1">{errors.email}</p>
          )}
        </div>

        <div>
          <label className="block text-sm mb-1 font-medium">Password</label>
          <input
            type="password"
            placeholder="Enter password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full border border-border rounded-md p-2 bg-background focus:outline-none focus:ring-2 focus:ring-primary"
          />
          {errors.password && (
            <p className="text-red-500 text-xs mt-1">{errors.password}</p>
          )}
        </div>

        <button
          type="submit"
          className="mt-2 w-full bg-primary text-primary-foreground py-2 rounded-md font-medium hover:bg-primary/90 transition"
        >
          {mode === "signup" ? "Create Account" : "Login"}
        </button>

        {mode === "login" && (
          <p className="text-center text-sm mt-3 text-muted-foreground">
            Don't have an account?{" "}
            <span
              onClick={() => setMode("signup")}
              className="text-primary underline cursor-pointer"
            >
              Sign up
            </span>
          </p>
        )}

        {mode === "signup" && (
          <p className="text-center text-sm mt-3 text-muted-foreground">
            Already have an account?{" "}
            <span
              onClick={() => setMode("login")}
              className="text-primary underline cursor-pointer"
            >
              Login here
            </span>
          </p>
        )}
      </form>
    </div>
  );
};

export default Login;