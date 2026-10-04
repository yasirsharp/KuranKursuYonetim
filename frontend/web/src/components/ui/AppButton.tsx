import React from 'react';

interface AppButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'outline';
  fullWidth?: boolean;
}

export function AppButton({ 
  children, 
  variant = 'primary', 
  fullWidth = false, 
  className = '', 
  style, 
  ...props 
}: AppButtonProps) {
  
  const getVariantStyles = () => {
    switch (variant) {
      case 'primary':
        return {
          background: 'var(--color-green)',
          color: 'white',
          border: 'none',
        };
      case 'secondary':
        return {
          background: 'var(--color-bg-inner)',
          color: 'var(--color-text)',
          border: '1px solid rgba(0,0,0,0.1)',
        };
      case 'danger':
        return {
          background: 'var(--color-red)',
          color: 'white',
          border: 'none',
        };
      case 'outline':
        return {
          background: 'transparent',
          color: 'var(--color-green)',
          border: '1px solid var(--color-green)',
        };
      default:
        return {};
    }
  };

  return (
    <button
      className={`app-button ${className}`}
      style={{
        padding: '12px 24px',
        borderRadius: 12,
        fontWeight: 700,
        fontSize: 16,
        cursor: props.disabled ? 'not-allowed' : 'pointer',
        opacity: props.disabled ? 0.6 : 1,
        transition: 'all 0.2s',
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 8,
        width: fullWidth ? '100%' : 'auto',
        ...getVariantStyles(),
        ...style
      }}
      {...props}
    >
      {children}
    </button>
  );
}
