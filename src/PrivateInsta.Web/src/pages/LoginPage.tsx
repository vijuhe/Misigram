export function LoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-pink-50 to-purple-50">
      <div className="bg-white rounded-2xl shadow-lg p-10 w-full max-w-sm text-center">
        <h1 className="text-3xl font-bold text-pink-600 mb-2">Misigram</h1>
        <p className="text-gray-500 text-sm mb-8">Our private space</p>
        <a
          href="/api/auth/login"
          className="flex items-center justify-center gap-3 w-full border border-gray-300 rounded-full py-3 text-gray-700 font-medium hover:bg-gray-50 transition"
        >
          <img src="https://www.google.com/favicon.ico" alt="Google" className="w-5 h-5" />
          Continue with Google
        </a>
      </div>
    </div>
  );
}
