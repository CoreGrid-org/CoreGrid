from app.scope import fallback_plan, rejected_plan, rejection_reason, validate_plan


def test_accepts_lifecycle_objective():
    assert rejection_reason("Evaluate whether the asset should be repaired or replaced") is None


def test_rejects_user_administration_before_openai():
    reason = rejection_reason("Create a new user and assign an Administrator role")
    assert reason is not None


def test_rejected_plan_has_no_steps():
    plan = rejected_plan("out of scope")
    assert plan.in_scope is False
    assert plan.steps == []
    validate_plan(plan)


def test_fallback_plan_is_typed_and_ordered():
    plan = fallback_plan()
    validate_plan(plan)
    assert [step.seq for step in plan.steps] == [1, 2, 3, 4]
