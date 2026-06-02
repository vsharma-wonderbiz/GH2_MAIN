import { NavLink } from "react-router-dom";
import {
  Home,
  Network,
  Cpu,
  Activity,
  FileText,
  Settings,
} from "lucide-react";
import logo from "../assets/Logo-removebg-preview.png"

const menuItems = [
  { icon: <Home size={18} />, label: "Dashboard", path: "/dashboard" },
  { icon: <Home size={18} />, label: "Performance", path: "/performance" },
  { icon: <Home size={18} />, label: "Signal", path: "/signals" }
];

export default function Sidebar() {
  return (
    <aside className="w-64 h-full bg-sidebar flex flex-col p-4 overflow-y-auto">
      {/* Brand / Logo */}
      {/* <div className="text-2xl font-bold mb-6 text-sidebar-primary tracking-wide">
        Tmind
      </div> */}
      <div className="h-12 flex items-center justify-center border-b border-sidebar-border px-4 mb-6">
      <div className="flex items-center gap-2">
            {/* <div className="w-8 h-8 bg-primary rounded flex items-center justify-center font-bold text-primary-foreground">
              T
            </div>
            <span className="font-bold text-lg text-sidebar-foreground">Tmind</span> */}
           <img src={logo} className="mb-5" alt="" />
      </div>
      </div>

      {/* Navigation Menu */}
      <nav className="space-y-2">
        {menuItems.map((item, idx) => (
          <NavLink
            key={idx}
            to={item.path}
            className={({ isActive }) =>
              `flex items-center gap-3 p-2 rounded-lg transition cursor-pointer font-semibold  ${
                isActive
                  ? "bg-background text-black"
                  : "hover:bg-sidebar-primary hover:text-sidebar-primary-foreground"
              }`
            }
          >
            {item.icon}
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
