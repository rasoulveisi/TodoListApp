# **3-Month Backend Readiness Checks**

## **1. Merge Request Quality (Main Evaluation)**

Rasoul should be able to create **production-ready Merge Requests** similar to other backend developers.

Each MR should show:

- correct architecture usage (Domain / Application / Infrastructure)
- efficient EF queries (no .AsEnumerable() misuse, no N+1 queries)
- DTO projection instead of returning entities
- proper validation and error handling
- clear MR description

**Expected outcome**

- MR passes **AI review and human review** with only minor comments.

---

# **2. Backend Feature Implementation**

Rasoul must be able to implement **backend features in Smart repositories** independently.

Examples:

- add new API endpoints
- extend existing business logic
- implement filtering or pagination
- implement validation rules

**Expected outcome**

- he can deliver a feature **from ticket → MR → merge** like other backend developers.

---

# **3. Support & Debugging Contribution**

Rasoul should be able to **assist support in diagnosing backend issues**.

He should be able to:

- read logs
- trace request flows
- identify problematic queries or logic
- explain where a failure originates

**Expected outcome**

- he can help the team investigate backend issues.

---

# **4. Frontend Responsibility**

Rasoul’s main focus should be **onboarding into backend development and architecture**.

Frontend implementation should **mostly be handled by AI tools or other developers**.

His involvement in frontend should mainly consist of:

- reviewing frontend backlogs
- ensuring API requirements are correct
- confirming backend contracts match frontend needs
- occasionally assisting with small frontend adjustments if needed

**Expected outcome**

- the majority of his effort is focused on **backend development**, while frontend work remains **aligned with backend APIs through his reviews**.

---

# **Final Success Criteria**

After 3 months Rasoul should be able to:

- implement backend features in **Smart repositories**
- create **reviewable merge requests**
- assist support with backend debugging
- review frontend backlogs while **keeping the main focus on backend development**.