FROM python:3.12
WORKDIR /app
COPY require.txt .
RUN pip install --no-cache-dir -r require.txt
COPY . .
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
