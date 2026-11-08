/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./projects/bank-app/src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      colors: {
        canvas: '#f6f7f9',
        line: '#e4e7ec',
        ink: {
          DEFAULT: '#101828',
          soft: '#475467',
          faint: '#667085',
        },
        brand: {
          DEFAULT: '#0f2d59',
          hover: '#1a3f78',
          tint: '#eef2f8',
        },
        accent: '#1d4ed8',
        positive: '#15803d',
      },
    },
  },
  plugins: [],
};
