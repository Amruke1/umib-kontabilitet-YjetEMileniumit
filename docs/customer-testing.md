# Customer Testing – Mini ERP

**Tester:** Al  
**Date:** 02.11.2026  
**Environment:** Local Development  
**Database:** PostgreSQL – mini_erp

## Test Case 1 – Successful Customer Creation

- **Input:** Arta Krasniqi, arta.test@example.com, 044123456
- **Expected Result:** The customer is successfully created and displayed in the customer list.
- **Actual Result:** The customer was successfully added and displayed in the list.
- **Status:** PASS

## Test Case 2 – Missing Customer Name

- **Input:** Empty name, test@example.com, 044111222
- **Expected Result:** The system prevents submission without a customer name.
- **Actual Result:** The browser displayed "Please fill out this field" and prevented submission.
- **Status:** PASS

## Test Case 3 – Invalid Email Address

- **Input:** Blerina Test, blerina.test, 044222333
- **Expected Result:** The system rejects an invalid email address.
- **Actual Result:** The browser displayed "Please include an @ in the email address".
- **Status:** PASS

## Test Case 4 – Data Persistence After Refresh

- **Action:** Refreshed the application using Ctrl + F5 after creating a customer.
- **Expected Result:** The previously created customer remains visible.
- **Actual Result:** Arta Krasniqi remained visible after refreshing the page.
- **Status:** PASS

## Test Case 5 – API Unavailable

- **Action:** Temporarily stopped the backend API and refreshed the frontend.
- **Expected Result:** The frontend displays an API connection error.
- **Actual Result:** The application displayed "API nuk është e qasshme".
- **Status:** PASS

## Final Summary

All five customer testing scenarios were executed successfully in the local development environment.

**Final Result: 5/5 tests passed.**