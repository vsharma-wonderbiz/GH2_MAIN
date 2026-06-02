FROM node:20

WORKDIR /app

# Copy package files first
COPY package*.json ./

# Install all dependencies from package.json
RUN npm install

# Copy source code
COPY . .

# Expose Vite port
EXPOSE 5173

# Start Vite development server
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]