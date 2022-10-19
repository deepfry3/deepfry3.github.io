#include "CollisionFunctions.h"
#include <algorithm>

// Calls the correct function according to the type of shape passed.
CollisionData CollideShapeToShape(const Shape& shapeA, const Shape& shapeB)
{
	// Square and...
	if (shapeA.GetType() == ShapeType::Square)
	{
		if (shapeB.GetType() == ShapeType::Square)
			return CollideSquareToSquare((Square&)shapeA, (Square&)shapeB);

		else if (shapeB.GetType() == ShapeType::Circle)
			return CollideSquareToCircle((Square&)shapeA, (Circle&)shapeB);

		else if (shapeB.GetType() == ShapeType::Plane)
			return CollideSquareToPlane((Square&)shapeA, (Plane&)shapeB);
	}

	// Circle and...
	else if (shapeA.GetType() == ShapeType::Circle)
	{
		if (shapeB.GetType() == ShapeType::Square)
			return CollideCircleToSquare((Circle&)shapeA, (Square&)shapeB);

		else if (shapeB.GetType() == ShapeType::Circle)
			return CollideCircleToCircle((Circle&)shapeA, (Circle&)shapeB);

		else if (shapeB.GetType() == ShapeType::Plane)
			return CollideCircleToPlane((Circle&)shapeA, (Plane&)shapeB);
	}

	// Plane and...
	else if (shapeA.GetType() == ShapeType::Plane)
	{
		if (shapeB.GetType() == ShapeType::Square)
			return CollideSquareToPlane((Square&)shapeB, (Plane&)shapeA);

		else if (shapeB.GetType() == ShapeType::Circle)
			return CollideCircleToPlane((Circle&)shapeB, (Plane&)shapeA);
	}

	return CollisionData();
}

// Return collision information for two potentially-colliding Squares.
CollisionData CollideSquareToSquare(const Square& squareA, const Square& squareB)
{
	CollisionData result;

	// Get the minimum and maximum coordinates of the two squares
	glm::vec2 squareAMin, squareAMax, squareBMin, squareBMax;
	squareA.GetCoordinates(&squareAMin, &squareAMax);
	squareB.GetCoordinates(&squareBMin, &squareBMax);
	
	// Calculate the square overlap distance on both axes, and store the minimum
	float xOverlap1 = squareAMax.x - squareBMin.x;
	float xOverlap2 = squareBMax.x - squareAMin.x;
	float yOverlap1 = squareAMax.y - squareBMin.y;
	float yOverlap2 = squareBMax.y - squareAMin.y;
	float minOverlap = std::min({ xOverlap1, xOverlap2, yOverlap1, yOverlap2 });

	// Calculate and store depth and normal based on the minimum overlap
	result.depth = minOverlap;
	result.normal =
		(minOverlap == xOverlap1) ? glm::vec2(1.0f, 0.0f) :
		(minOverlap == xOverlap2) ? glm::vec2(-1.0f, 0.0f) :
		(minOverlap == yOverlap1) ? glm::vec2(0.0f, 1.0f) :
		glm::vec2(0.0f, -1.0f);

	// Calculate and store the mid point position based on an average
	result.worldPos.x = (std::min(squareAMax.x, squareBMax.x) + std::max(squareAMin.x, squareBMin.x)) / 2.0f;
	result.worldPos.y = (std::min(squareAMax.y, squareBMax.y) + std::max(squareAMin.y, squareBMin.y)) / 2.0f;

	result.shapeA = (Shape*)&squareA;
	result.shapeB = (Shape*)&squareB;
	return result;
}

// Return collision information for a potentially-colliding Square and Circle.
CollisionData CollideSquareToCircle(const Square& square, const Circle& circle)
{
	return CollideCircleToSquare(circle, square);
}

// Return collision information for a potentially-colliding Square and Plane.
CollisionData CollideSquareToPlane(const Square& square, const Plane& plane)
{
	CollisionData result;

	// Get square coordinates and project each corner onto the plane
	glm::vec2 squareMin = square.GetMinCoordinates();
	glm::vec2 squareMax = square.GetMaxCoordinates();
	float normalProjectionXminYmin = glm::dot(squareMin, plane.normal);
	float normalProjectionXminYmax = glm::dot({ squareMin.x, squareMax.y }, plane.normal);
	float normalProjectionXmaxYmin = glm::dot({ squareMax.x, squareMin.y }, plane.normal);
	float normalProjectionXmaxYmax = glm::dot(squareMax, plane.normal);

	// Calculate world position
	// This section has been commented out as it's not required, but it depends on which normal projection
	// is the longest. For example:
	// result.worldPos = glm::vec2(squareMax.x, squareMin.y) - projectedPos + (plane.normal * plane.offset);

	// Store collision information
	result.normal = -plane.normal;
	result.depth = std::max({
		-(normalProjectionXminYmin - (plane.offset)),
		-(normalProjectionXminYmax - (plane.offset)),
		-(normalProjectionXmaxYmin - (plane.offset)),
		-(normalProjectionXmaxYmax - (plane.offset))
		});

	// Return result
	result.shapeA = (Shape*)&square;
	result.shapeB = (Shape*)&plane;
	return result;
}

// Return collision information for a potentially-colliding Circle and Square.
CollisionData CollideCircleToSquare(const Circle& circle, const Square& square)
{
	CollisionData result;

	// Get the minimum and maximum coordinates of the square
	glm::vec2 squareMin, squareMax;
	square.GetCoordinates(&squareMin, &squareMax);

	// Clamp circle's position to square's bounds, and draw a cross
	glm::vec2 clampedPos = circle.position;
	if (clampedPos.x < squareMin.x) clampedPos.x = squareMin.x;
	if (clampedPos.y < squareMin.y) clampedPos.y = squareMin.y;
	if (clampedPos.x > squareMax.x) clampedPos.x = squareMax.x;
	if (clampedPos.y > squareMax.y) clampedPos.y = squareMax.y;

	// Calculate and collision information accounting for overlap
	glm::vec2 circlePosToClampedPos = clampedPos - circle.position;
	float distance = glm::length(circlePosToClampedPos);
	if (distance <= 0.0f)
	{
		glm::vec2 relativePos = square.position - circle.position;
		if (relativePos.x == relativePos.y)
		{
			result.depth = glm::length(relativePos);
			result.normal = relativePos;
		}
		else if (abs(relativePos.x) > abs(relativePos.y))
		{
			result.depth = abs(relativePos.y);
			result.normal = glm::normalize(glm::vec2(0.0f, relativePos.y));
		}
		else
		{
			result.depth = abs(relativePos.x);
			result.normal = glm::normalize(glm::vec2(relativePos.x, 0.0f));
		}
	}
	else
	{
		result.depth = circle.radius - distance;
		result.normal = circlePosToClampedPos / distance;
	}

	// Store remaining collision information
	result.worldPos = clampedPos;
	result.shapeA = (Shape*)&circle;
	result.shapeB = (Shape*)&square;
	return result;
}

// Return collision information for two potentially-colliding Circles.
CollisionData CollideCircleToCircle(const Circle& circleA, const Circle& circleB)
{
	CollisionData result;

	// Get the circles' position difference and magnitude
	glm::vec2 difference = circleB.position - circleA.position;
	float distance = glm::length(difference);

	// Calculate and store collision information
	result.depth = circleA.radius + circleB.radius - distance;
	result.normal = difference / distance;
	result.worldPos = circleB.position - result.normal;

	result.shapeA = (Shape*)&circleA;
	result.shapeB = (Shape*)&circleB;
	return result;
}

// Return collision information for a potentially-colliding Circle and Plane.
CollisionData CollideCircleToPlane(const Circle& circle, const Plane& plane)
{
	CollisionData result;

	// Project the circle onto the plane
	float normalProjection = glm::dot(circle.position, plane.normal);
	glm::vec2 projectedPosition = normalProjection * plane.normal;

	// Store the resulting collision information
	result.normal = -plane.normal;
	result.worldPos = circle.position - projectedPosition + (plane.normal * plane.offset);
	result.depth = -(normalProjection - (plane.offset + circle.radius));

	// Return information
	result.shapeA = (Shape*)&circle;
	result.shapeB = (Shape*)&plane;
	return result;
}