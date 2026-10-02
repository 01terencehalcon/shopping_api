import { Inter } from "next/font/google";
import "./globals.css";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
  display: "swap",
  preload: false,
});

export const metadata = {
  title: "STYLESHOP | Online Clothes Shopping System",
  description: "Modern, full-stack online clothes shopping system built with Next.js, Tailwind CSS, and ASP.NET Core Web API.",
  keywords: ["clothes", "shopping", "fashion", "online store", "styleshop"],
};

export default function RootLayout({ children }) {
  return (
    <html
      lang="en"
      className={`${inter.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-gray-50 text-gray-900 font-sans">
        {children}
      </body>
    </html>
  );
}
