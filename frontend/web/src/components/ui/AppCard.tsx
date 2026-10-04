import React from 'react';

interface AppCardProps {
  children: React.ReactNode;
  className?: string;
  style?: React.CSSProperties;
  onClick?: () => void;
}

export function AppCard({ children, className = '', style, onClick }: AppCardProps) {
  return (
    <div
      className={`glass-card ${className}`}
      style={{
        background: 'var(--color-surface)',
        borderRadius: 16,
        boxShadow: '0 4px 20px rgba(0, 0, 0, 0.04)',
        border: '1px solid rgba(255, 255, 255, 0.5)',
        padding: 16,
        ...style
      }}
      onClick={onClick}
    >
      {children}
    </div>
  );
}
