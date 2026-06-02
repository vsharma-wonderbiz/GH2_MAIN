import type { Mapping } from "../../api/assetApi";
import { useState, useEffect, useRef } from "react";
import { toast } from "react-toastify";

export function SignalMultiSelect({
  signals,
  selectedSignals,
  onChange,
  disabled,
}: {
  signals: Mapping[];
  selectedSignals: string[];
  onChange: (selected: string[]) => void;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const wrapperRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const filtered = signals.filter((s) =>
    s.tagName.toLowerCase().includes(search.toLowerCase())
  );


  const toggle = (tagName: string) => {
    if (selectedSignals.includes(tagName)) {
      onChange(selectedSignals.filter((s) => s !== tagName));
    } else {
      if (selectedSignals.length >= 3) {
        toast.warning("Maximum of # signal can be selected");
        return;
      }
      onChange([...selectedSignals, tagName]);
    }
  };

  
 

  return (
    <div className="flex flex-1 flex-col gap-1 relative min-w-[200px]" ref={wrapperRef}>
      <label className="text-sm text-gray-500">Signals</label>

      {/* Trigger */}
      <button
        type="button"
        disabled={disabled || signals.length === 0}
        onClick={() => setOpen((v) => !v)}
        className="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700
          text-left flex items-center justify-between text-sm text-gray-700 dark:text-gray-200
          hover:border-blue-400 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <span className="truncate">
          {selectedSignals.length === 0
            ? "Select Signals"
            : `${selectedSignals.length} signal${selectedSignals.length > 1 ? "s" : ""} selected`}
        </span>
        <div className="flex items-center gap-1.5 flex-shrink-0">
          {selectedSignals.length > 0 && (
            <span className="bg-blue-500 text-white text-xs font-semibold rounded-full px-2 py-0.5">
              {selectedSignals.length}
            </span>
          )}
          <svg
            className={`w-4 h-4 text-gray-400 transition-transform ${open ? "rotate-180" : ""}`}
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M6 9l6 6 6-6" />
          </svg>
        </div>
      </button>

      {/* Dropdown panel */}
      {open && (
        <div className="absolute top-full left-0 right-0 mt-1 z-50 bg-white dark:bg-gray-800
          border border-gray-200 dark:border-gray-600 rounded-xl shadow-xl overflow-hidden">

          {/* Search */}
          <div className="p-2 border-b border-gray-100 dark:border-gray-700">
            <input
              autoFocus
              type="text"
              placeholder="Search signals…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full px-3 py-1.5 text-sm rounded-lg border border-gray-200 dark:border-gray-600
                bg-gray-50 dark:bg-gray-700 text-gray-700 dark:text-gray-200
                focus:outline-none focus:border-blue-400"
            />
          </div>

          {/* List */}
          <div className="max-h-52 overflow-y-auto">
            {filtered.length === 0 ? (
              <p className="px-3 py-3 text-sm text-gray-400 text-center">No signals found</p>
            ) : (
              filtered.map((signal) => {
                const isChecked = selectedSignals.includes(signal.tagName);
                const isDisabledOption =
                  !isChecked && selectedSignals.length >= 3;

                return (
                  <div
                    key={signal.mappingId}
                    onClick={() => !isDisabledOption && toggle(signal.tagName)}
                    className={`flex items-center gap-2.5 px-3 py-2 text-sm transition-colors
                      ${
                        isDisabledOption
                          ? "opacity-50 cursor-not-allowed"
                          : "cursor-pointer"
                      }
                      ${
                        isChecked
                          ? "bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 font-medium"
                          : "text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700"
                      }`}
                  >
                    <input
                      type="checkbox"
                      checked={isChecked}
                      disabled={isDisabledOption}
                      onChange={() => toggle(signal.tagName)}
                      onClick={(e) => e.stopPropagation()}
                      className="accent-blue-500 w-3.5 h-3.5 flex-shrink-0"
                    />
                    <span className="truncate">{signal.tagName}</span>
                  </div>
                );
              })
            )}
          </div>

          {/* Footer */}
          {/* <div className="flex items-center justify-between px-3 py-2
            border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
            <span className="text-xs text-gray-400">
              {selectedSignals.length} of {signals.length} selected
            </span>
            <div className="flex gap-2 items-center">
              <button
                onClick={selectAll}
                className="text-xs text-blue-500 hover:text-blue-600 font-medium"
              >
                Select all
              </button>
              <span className="text-gray-300 dark:text-gray-600">|</span>
              <button
                onClick={clearAll}
                className="text-xs text-gray-400 hover:text-red-500 font-medium"
              >
                Clear
              </button>
            </div>
          </div> */}
        </div>
      )}

      {/* Selected signal pills */}
      {selectedSignals.length > 0 && (
        <div className="flex flex-wrap gap-1 mt-1">
          {selectedSignals.map((sig) => (
            <span
              key={sig}
              className="inline-flex items-center gap-1 bg-blue-50 dark:bg-blue-900/30
                text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-blue-700
                text-xs rounded-full px-2 py-0.5"
            >
              {sig}
              <button
                onClick={() => toggle(sig)}
                className="text-blue-400 hover:text-red-500 transition-colors leading-none ml-0.5"
              >
                ✕
              </button>
            </span>
          ))}
        </div>
      )}
    </div>
  );
}