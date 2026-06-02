import { Bell, User, Menu } from "lucide-react";
import ThemeToggle from "./ThemeToggle";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Badge } from "@/components/ui/badge";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import { toast } from "react-toastify";

interface TopbarProps {
  onToggleSidebar?: () => void;
}

const api = axios.create({
  baseURL: "https://localhost:7144/api",
  withCredentials: true, 
  headers: {
    "Content-Type": "application/json",
  },
});

export default function Topbar({ onToggleSidebar }: TopbarProps) {
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await api.post("/User/logout");
      toast.success("Logged Out Sucessfully");
    } catch (err) {
      console.error("Logout failed:", err);
    } finally {
      navigate("/");
    }
  };

  return (
    <header className="sticky top-0 z-40 h-16 flex items-center justify-between px-4 sm:px-6 bg-card backdrop-blur-md border-b border-border shadow-sm transition-colors">
      {/* Left side: Hamburger + Title */}
      <div className="flex items-center gap-3">
        <Button
          variant="ghost"
          size="icon"
          className="lg:hidden"
          onClick={onToggleSidebar}
        >
          <Menu className="h-6 w-6 text-foreground" />
        </Button>

        <h1 className="text-lg font-semibold text-foreground tracking-tight">
          <span className="lg:hidden">Gh2</span>
          <span className="hidden lg:inline">Green Hydrogen Monitoring System</span>
        </h1>
      </div>

      {/* Right side actions */}
      <div className="flex items-center gap-3">
        {/* <ThemeToggle /> */}

        {/* Notifications */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="relative hover:bg-accent/30 transition-colors rounded-full"
            >
              <Bell className="h-5 w-5 text-foreground" />
              <Badge className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 bg-destructive text-destructive-foreground text-xs font-medium shadow">
                3
              </Badge>
            </Button>
          </DropdownMenuTrigger>

          <DropdownMenuContent
            align="end"
            className="w-80 bg-card border border-border shadow-lg rounded-lg p-2"
          >
            <DropdownMenuLabel>Notifications</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {[
              { title: "High Temperature Alert", desc: "Asset PLC-001 exceeds threshold" },
              { title: "Device Offline", desc: "Gateway-03 disconnected" },
              { title: "Maintenance Due", desc: "Asset M-204 scheduled today" },
            ].map((n, i) => (
              <DropdownMenuItem key={i} className="flex flex-col gap-1 px-2 py-2">
                <p className="text-sm font-medium">{n.title}</p>
                <p className="text-xs text-muted-foreground">{n.desc}</p>
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* User Menu */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              className="flex items-center gap-2 rounded-full px-2 py-1.5 hover:bg-accent/30 transition-colors"
            >
              <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center shadow-sm">
                <User className="h-4 w-4 text-primary-foreground" />
              </div>
              <span className="text-sm font-medium text-foreground hidden sm:inline">
                Admin User
              </span>
            </Button>
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end" className="w-48 bg-card border border-border">
            <DropdownMenuLabel>My Account</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem>Profile</DropdownMenuItem>
            <DropdownMenuItem>Settings</DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onClick={handleLogout}
              className="text-destructive font-medium"
            >
              Logout
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}