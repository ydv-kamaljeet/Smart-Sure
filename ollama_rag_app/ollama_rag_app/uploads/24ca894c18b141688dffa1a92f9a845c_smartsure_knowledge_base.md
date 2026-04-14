# SmartSure: Essential Insurance Knowledge Base

This document provides a comprehensive guide to understanding insurance concepts, calculations, and the services provided by SmartSure.

---

## 1. Key Insurance Definitions

### What is a Premium?
The **Premium** is the fixed amount of money you pay regularly to the insurance company to keep your insurance policy active. It is essentially the "subscription fee" for your coverage. If you stop paying the premium, your policy may be cancelled, and you will no longer be covered.

### What is IDV (Insured Declared Value)?
**IDV** stands for **Insured Declared Value**. It is the maximum amount that the insurance company is liable to pay you in the event of a "Total Loss" (e.g., your car is stolen or your home is completely destroyed). 
*   **Market Value**: It represents the current market value of your asset.
*   **Depreciation**: As assets get older, their value decreases. IDV is calculated by subtracting depreciation from the original price of the asset.

---

## 2. Types of Insurance Provided

SmartSure currently offers two main categories of insurance, with various customizable plans:

### 2.1 Vehicle Insurance
Coverage for your cars, bikes, and other motor vehicles. 
*   **Subtypes**: Comprehensive, Third-Party, Zero Depreciation, and more.
*   **Coverage**: Protects against theft, accidents, and third-party liabilities.

### 2.2 Home Insurance
Coverage for your property and residential buildings.
*   **Subtypes**: Structure Only, Content Only, or Combined (Structure + Content).
*   **Coverage**: Protects against natural disasters (fire, flood, earthquake), theft/burglary, and accidental damage.

---

## 3. Calculation Logic

SmartSure uses standardized mathematical formulas to ensure fair pricing and valuation.

### 3.1 Vehicle Insurance Calculations

#### IDV Calculation (Vehicle)
The IDV is calculated based on the age of the vehicle.
1.  **Calculate Age**: `Age = Current Year - Manufacturing Year`
2.  **Apply Depreciation**:
    *   Less than 1 year: **5%**
    *   1 to 2 years: **15%**
    *   2 to 3 years: **20%**
    *   3 to 4 years: **30%**
    *   4 to 5 years: **40%**
    *   5 years or more: **50%**
3.  **Formula**: `IDV = Original Listed Price × (1 - Depreciation %)`

#### Premium Calculation (Vehicle)
1.  **Base Premium**: `Annual Premium = IDV × 2%`
2.  **High Mileage Surcharge**: If you drive more than **15,000 km per year**, a **20% surcharge** is added to the premium to account for higher wear and tear.

---

### 3.2 Home Insurance Calculations

#### IDV Calculation (Home)
The IDV for homes is based on the property value and the age of the building.
1.  **Calculate Age**: `Age = Current Year - Year Built`
2.  **Apply Depreciation**:
    *   Less than 5 years: **0%**
    *   5 to 10 years: **10%**
    *   10 to 20 years: **20%**
    *   20 to 30 years: **30%**
    *   30 years or more: **40%**
3.  **Formula**: `IDV = Current Property Value × (1 - Depreciation %)`

#### Premium Calculation (Home)
1.  **Base Premium**: `Annual Premium = IDV × 0.1%` (e.g., $100 for every $100,000 of coverage).
2.  **Security Discount**: If your home is equipped with a **Certified Security System**, you receive a **10% discount** on your premium because the risk of theft is lower.

---

## 4. The Claims Process

1.  **Initiation**: When an incident occurs, you "File a Claim" through the SmartSure app.
2.  **Validation**: Our system checks if your policy is **Active**.
    *   *Note: You cannot file claims on cancelled or expired policies.*
3.  **Assessment**: An admin reviews the description and requested **Claim Amount**.
    *   *Rule: The Claim Amount must be less than or equal to the policy's IDV.*
4.  **Decision**: The claim is either **Approved** (payout sent) or **Rejected** (reason sent via email).

---

## 5. Contact & Support
If you have further questions or need technical assistance with your policy, please use the **Chat Assistant** or contact our support team at `support@smartsure.com`.
