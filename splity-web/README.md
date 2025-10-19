# Splity Web Frontend

Modern Next.js frontend for the Splity expense sharing application.

## 🚀 Quick Start

1. **Install dependencies**:
   ```bash
   pnpm install
   ```

2. **Start development server**:
   ```bash
   pnpm run dev
   ```

3. **Build for production**:
   ```bash
   pnpm run build
   pnpm run start
   ```

## 🔧 Configuration

The app uses environment variables defined in `.env.local`:

```env
NEXT_PUBLIC_API_BASE_URL=https://sicb7dcxbd.execute-api.eu-west-2.amazonaws.com/dev
```

## 🏗️ Architecture

### Tech Stack
- **Framework**: Next.js 15 with App Router
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **UI Components**: Custom component library (Radix UI + shadcn/ui)
- **Icons**: Lucide React
- **State Management**: React Context + hooks

### Key Features
- ✅ **User Management**: User creation, authentication context
- ✅ **Party Management**: Create parties, basic CRUD operations  
- ✅ **API Integration**: Connected to deployed AWS Lambda backend
- ✅ **Type Safety**: Full TypeScript integration with backend models
- ✅ **Responsive Design**: Mobile-first approach
- ✅ **Modern UI**: shadcn/ui component library

### API Integration Status
- ✅ **User APIs**: Create, get, update users
- ✅ **Party APIs**: Create, get, update, delete parties
- ✅ **Receipt Upload**: File upload to S3 with AI processing
- ⚠️ **Expense APIs**: Create, delete (update not implemented in backend)
- ❌ **Party Lists**: Backend endpoint not implemented
- ❌ **Member Management**: Backend endpoints not implemented
- ❌ **Settlement Calculation**: Backend logic not implemented

## 📁 Project Structure

```
splity-web/
├── app/                    # Next.js App Router pages
│   ├── dashboard/         # Main dashboard and party management
│   └── layout.tsx         # Root layout with providers
├── components/            # React components
│   ├── ui/               # Base UI components (shadcn/ui)
│   ├── create-party-dialog.tsx
│   ├── party-expenses.tsx
│   └── ...
├── contexts/             # React contexts
│   └── auth-context.tsx # User authentication state
├── hooks/                # Custom React hooks
│   ├── use-parties.ts   # Party management
│   └── use-party.ts     # Single party operations
├── lib/                  # Utilities
│   └── api-client.ts    # HTTP client for backend APIs
├── services/            # API service layers
│   ├── party-service.ts
│   ├── user-service.ts
│   ├── expense-service.ts
│   └── settlement-service.ts
├── types/               # TypeScript type definitions
│   └── index.ts        # Core types matching backend models
└── styles/             # Global styles
```

## 🔄 Recent Updates

### ✅ Completed Improvements
1. **API Integration**: Updated to use deployed AWS Lambda backend
2. **Type Alignment**: Fixed TypeScript types to match C# backend models
3. **Response Format**: Updated to handle backend's `{success, data, errorMessage}` format
4. **User Context**: Added proper user management with backend integration
5. **Party Creation**: Working create party flow with proper validation
6. **Environment Setup**: Configured API URL and build process

### 🚧 Currently Working
- Basic party and user management functionality
- Receipt upload UI (backend integration ready)
- Error handling and loading states

### 📝 TODO (Missing Backend Features)
See main [README.md](../README.md#todo-missing-backend-features) for comprehensive list of backend features that need implementation.

## 🧪 Testing

The frontend has been tested with the deployed backend:
- ✅ User creation and retrieval
- ✅ Party creation with proper owner assignment
- ✅ API error handling
- ✅ Build process and development server

## 🚀 Deployment

The frontend is ready for deployment to platforms like:
- Vercel (recommended for Next.js)
- Netlify
- AWS Amplify
- Custom server

Make sure to set the `NEXT_PUBLIC_API_BASE_URL` environment variable to your deployed backend URL.

## 🐛 Known Issues

1. **Party List Loading**: Currently loads from user details instead of dedicated endpoint (backend limitation)
2. **Member Management**: UI exists but backend endpoints not implemented
3. **Settlement Calculations**: Frontend ready but backend logic missing
4. **Real-time Updates**: No WebSocket integration yet

## 🤝 Contributing

1. Check the main TODO list for missing backend features
2. Focus on frontend improvements and UX enhancements
3. Ensure TypeScript types stay aligned with backend models
4. Follow the existing code patterns and architecture